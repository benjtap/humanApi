using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Webhttp.Models;

namespace SelfApiproj.Services
{

    public interface IHybridSearchService
    {
        Task<HybridSearchResult> SearchHybridAsync(HybridSearchRequest request);
        Task<List<ImageDocument>> SearchVectorOnlyAsync(string query, int limit = 10, float threshold = 0.7f);
        Task<List<ImageDocument>> SearchTextOnlyAsync(string query, int limit = 10);
        Task<HybridSearchResult> SearchWithFiltersAsync(HybridSearchRequest request, SearchFilters filters);
    }
    public class HybridSearchService : IHybridSearchService
    {
        private readonly IMongoCollection<ImageDocument> _collection;
        private readonly IEmbeddingService _embeddingService;
        private readonly IImageFileService _imageFileService;
        private readonly ILogger<HybridSearchService> _logger;
        private readonly MongoImageOptions _options;

        public HybridSearchService(
            IOptions<MongoImageOptions> options,
            IEmbeddingService embeddingService,
            IImageFileService imageFileService,
            ILogger<HybridSearchService> logger)
        {
            _options = options.Value;
            var client = new MongoClient(_options.ConnectionString);
            var database = client.GetDatabase(_options.DatabaseName);
            _collection = database.GetCollection<ImageDocument>(_options.CollectionName);

            _embeddingService = embeddingService;
            _imageFileService = imageFileService;
            _logger = logger;
        }

        public async Task<HybridSearchResult> SearchHybridAsync(HybridSearchRequest request)
        {
            var startTime = DateTime.UtcNow;
            var metrics = new SearchMetrics();

            try
            {
                _logger.LogInformation("Début recherche hybride pour: {Query}", request.Query);

                // 1. Recherche vectorielle et textuelle en parallèle
                var vectorTask = SearchVectorWithMetricsAsync(request.Query, request.Limit * 2, request.VectorThreshold);
                var textTask = SearchTextWithMetricsAsync(request.Query, request.Limit * 2);

                var results = await Task.WhenAll(vectorTask, textTask);

                var (vectorResults, vectorDuration) = results[0];
                var (textResults, textDuration) = results[1];

                metrics.VectorSearchDuration = vectorDuration;
                metrics.TextSearchDuration = textDuration;
                metrics.VectorResults = vectorResults.Count;
                metrics.TextResults = textResults.Count;

                // 2. Fusion et scoring des résultats
                var mergedResults = await MergeAndScoreResults(
                    vectorResults, textResults, request.VectorWeight, request.TextWeight);
                metrics.MergedResults = mergedResults.Count;

                // 3. Application des filtres
                if (request.Filters != null)
                {
                    mergedResults = ApplyFilters(mergedResults, request.Filters);
                }
                metrics.FilteredResults = mergedResults.Count;

                // 4. Limitation et tri final
                var finalResults = mergedResults
                    .OrderByDescending(r => r.Score.TotalScore)
                    .Take(request.Limit)
                    .ToList();

                // 5. Enrichissement des résultats
                if (request.IncludeMetadata || request.HighlightMatches)
                {
                    await EnrichResults(finalResults, request);
                }

                metrics.TotalDuration = DateTime.UtcNow - startTime;

                // 6. Génération de suggestions
                var suggestions = await GenerateSuggestions(request.Query, finalResults.Count);

                _logger.LogInformation("Recherche hybride terminée: {Count} résultats en {Duration}ms",
                    finalResults.Count, metrics.TotalDuration.TotalMilliseconds);

                return new HybridSearchResult
                {
                    Success = true,
                    Query = request.Query,
                    TotalResults = finalResults.Count,
                    Results = finalResults,
                    Metrics = metrics,
                    Suggestions = suggestions
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche hybride pour: {Query}", request.Query);

                return new HybridSearchResult
                {
                    Success = false,
                    Query = request.Query,
                    ErrorMessage = ex.Message,
                    Metrics = metrics
                };
            }
        }

        public async Task<List<ImageDocument>> SearchVectorOnlyAsync(string query, int limit = 10, float threshold = 0.7f)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ImageDocument>();

            var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
            var queryVector = queryEmbedding.Select(d => (float)d).ToArray();

            var pipeline = new[]
            {
            new BsonDocument("$vectorSearch", new BsonDocument
            {
                { "index", _options.VectorIndexName },
                { "path", "TextEmbedding" },
                { "queryVector", new BsonArray(queryVector) },
                { "numCandidates", limit * 10 },
                { "limit", limit }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "vectorScore", new BsonDocument("$meta", "vectorSearchScore") }
            }),
            new BsonDocument("$match", new BsonDocument
            {
                { "vectorScore", new BsonDocument("$gte", threshold) }
            })
        };

            return await _collection.Aggregate<ImageDocument>(pipeline).ToListAsync();
        }

        public async Task<List<ImageDocument>> SearchTextOnlyAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<ImageDocument>();

            // Recherche textuelle avec MongoDB Atlas Search
            var pipeline = new[]
            {
            new BsonDocument("$search", new BsonDocument
            {
                { "index", "image_text_search" },
                { "text", new BsonDocument
                    {
                        { "query", query },
                        { "path", "ExtractedText" },
                        { "fuzzy", new BsonDocument("maxEdits", 1) }
                    }
                }
            }),
            new BsonDocument("$addFields", new BsonDocument
            {
                { "textScore", new BsonDocument("$meta", "searchScore") }
            }),
            new BsonDocument("$limit", limit)
        };

            return await _collection.Aggregate<ImageDocument>(pipeline).ToListAsync();
        }

        public async Task<HybridSearchResult> SearchWithFiltersAsync(HybridSearchRequest request, SearchFilters filters)
        {
            request.Filters = filters;
            return await SearchHybridAsync(request);
        }

        // Méthodes privées pour l'implémentation

        private async Task<(List<ImageDocument> Results, TimeSpan Duration)> SearchVectorWithMetricsAsync(
            string query, int limit, float threshold)
        {
            var start = DateTime.UtcNow;
            var results = await SearchVectorOnlyAsync(query, limit, threshold);
            var duration = DateTime.UtcNow - start;
            return (results, duration);
        }

        private async Task<(List<ImageDocument> Results, TimeSpan Duration)> SearchTextWithMetricsAsync(
            string query, int limit)
        {
            var start = DateTime.UtcNow;
            var results = await SearchTextOnlyAsync(query, limit);
            var duration = DateTime.UtcNow - start;
            return (results, duration);
        }

        private async Task<List<HybridResultItem>> MergeAndScoreResults(
            List<ImageDocument> vectorResults, List<ImageDocument> textResults,
            float vectorWeight, float textWeight)
        {
            var resultMap = new Dictionary<string, HybridResultItem>();

            // Traitement des résultats vectoriels
            foreach (var doc in vectorResults)
            {
                var vectorScore = GetVectorScore(doc);
                var item = await CreateHybridResultItem(doc);
                item.Score.VectorScore = vectorScore;
                item.Score.MatchType = "vector";

                resultMap[doc.DocumentId] = item;
            }

            // Traitement des résultats textuels
            foreach (var doc in textResults)
            {
                var textScore = GetTextScore(doc);

                if (resultMap.ContainsKey(doc.DocumentId))
                {
                    // Fusion des scores pour les documents trouvés par les deux méthodes
                    var existing = resultMap[doc.DocumentId];
                    existing.Score.TextScore = textScore;
                    existing.Score.MatchType = "both";
                }
                else
                {
                    // Nouveau document trouvé uniquement par recherche textuelle
                    var item = await CreateHybridResultItem(doc);
                    item.Score.TextScore = textScore;
                    item.Score.MatchType = "text";

                    resultMap[doc.DocumentId] = item;
                }
            }

            // Calcul du score final avec bonus
            foreach (var item in resultMap.Values)
            {
                item.Score.ConfidenceBonus = CalculateConfidenceBonus(item.OcrConfidence);
                item.Score.RecencyBonus = CalculateRecencyBonus(item.UploadedAt);

                item.Score.TotalScore =
                    (item.Score.VectorScore * vectorWeight) +
                    (item.Score.TextScore * textWeight) +
                    item.Score.ConfidenceBonus +
                    item.Score.RecencyBonus;
            }

            return resultMap.Values.ToList();
        }

        private async Task<HybridResultItem> CreateHybridResultItem(ImageDocument doc)
        {
            var imageUrl = await _imageFileService.GetImageUrlAsync(doc.RelativePath);
            var thumbnailUrl = await _imageFileService.GetImageUrlAsync(doc.ThumbnailPath);

            return new HybridResultItem
            {
                DocumentId = doc.DocumentId,
                NumeroDossier = doc.NumeroDossier,
                OriginalFileName = doc.OriginalFileName,
                ImageUrl = imageUrl,
                ThumbnailUrl = thumbnailUrl,
                ExtractedText = doc.ExtractedText,
                OcrConfidence = doc.OcrConfidence,
                UploadedAt = doc.UploadedAt,
                FileSize = doc.FileSizeBytes,
                Metadata = doc.Metadata
            };
        }

        private double GetVectorScore(ImageDocument doc)
        {
            // Le score vectoriel est fourni par MongoDB Atlas Vector Search
            return doc.GetType().GetProperty("vectorScore")?.GetValue(doc) as double? ?? 0.0;
        }

        private double GetTextScore(ImageDocument doc)
        {
            // Le score textuel est fourni par MongoDB Atlas Text Search
            return doc.GetType().GetProperty("textScore")?.GetValue(doc) as double? ?? 0.0;
        }

        private double CalculateConfidenceBonus(double ocrConfidence)
        {
            // Bonus basé sur la confiance OCR (max 0.1)
            return Math.Min(ocrConfidence * 0.1, 0.1);
        }

        private double CalculateRecencyBonus(DateTime uploadDate)
        {
            // Bonus pour les documents récents (max 0.05)
            var daysSinceUpload = (DateTime.UtcNow - uploadDate).TotalDays;
            if (daysSinceUpload <= 7) return 0.05;      // Cette semaine
            if (daysSinceUpload <= 30) return 0.03;     // Ce mois
            if (daysSinceUpload <= 90) return 0.01;     // Ce trimestre
            return 0;
        }

        private List<HybridResultItem> ApplyFilters(List<HybridResultItem> results, SearchFilters filters)
        {
            var filtered = results.AsQueryable();

            if (!string.IsNullOrEmpty(filters.NumeroDossier))
                filtered = filtered.Where(r => r.NumeroDossier == filters.NumeroDossier);

            if (filters.StartDate.HasValue)
                filtered = filtered.Where(r => r.UploadedAt >= filters.StartDate.Value);

            if (filters.EndDate.HasValue)
                filtered = filtered.Where(r => r.UploadedAt <= filters.EndDate.Value);

            if (filters.MinConfidence.HasValue)
                filtered = filtered.Where(r => r.OcrConfidence >= filters.MinConfidence.Value);

            if (filters.MaxConfidence.HasValue)
                filtered = filtered.Where(r => r.OcrConfidence <= filters.MaxConfidence.Value);

            if (filters.MinFileSize.HasValue)
                filtered = filtered.Where(r => r.FileSize >= filters.MinFileSize.Value);

            if (filters.MaxFileSize.HasValue)
                filtered = filtered.Where(r => r.FileSize <= filters.MaxFileSize.Value);

            if (filters.Categories?.Any() == true)
            {
                filtered = filtered.Where(r =>
                    r.Metadata.ContainsKey("categorie") &&
                    filters.Categories.Contains(r.Metadata["categorie"].ToString()));
            }

            return filtered.ToList();
        }

        private async Task EnrichResults(List<HybridResultItem> results, HybridSearchRequest request)
        {
            if (request.HighlightMatches)
            {
                foreach (var result in results)
                {
                    result.HighlightedText = HighlightMatches(result.ExtractedText, request.Query);
                }
            }
        }

        private string HighlightMatches(string text, string query)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(query))
                return text;

            var words = query.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var highlighted = text;

            foreach (var word in words)
            {
                var pattern = $@"\b{Regex.Escape(word)}\b";
                highlighted = Regex.Replace(highlighted, pattern,
                    $"<mark>$0</mark>", RegexOptions.IgnoreCase);
            }

            return highlighted;
        }

        private async Task<SearchSuggestions> GenerateSuggestions(string query, int resultCount)
        {
            var suggestions = new SearchSuggestions();

            if (resultCount == 0)
            {
                // Suggérer des termes alternatifs basés sur les documents existants
                var pipeline = new[]
                {
                new BsonDocument("$sample", new BsonDocument("size", 100)),
                new BsonDocument("$project", new BsonDocument("ExtractedText", 1)),
                new BsonDocument("$limit", 10)
            };

                var sampleDocs = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

                // Extraction de termes fréquents (implémentation simplifiée)
                var words = new List<string>();
                foreach (var doc in sampleDocs)
                {
                    if (doc.Contains("ExtractedText"))
                    {
                        var text = doc["ExtractedText"].AsString;
                        words.AddRange(text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Where(w => w.Length > 3)
                            .Take(5));
                    }
                }

                suggestions.RelatedTerms = words.Distinct().Take(5).ToList();
                suggestions.DidYouMean = true;
            }

            return suggestions;
        }

    }

}
