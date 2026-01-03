using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Webhttp.Models;

namespace SelfApiproj.Services
{
    // Interface du service
    public interface IFileVectorService
    {
        Task<string> UploadAndVectorizeFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string documentId,
            string numeroDossier,
            Dictionary<string, object>? additionalMetadata = null);

        Task<List<VectorDocument>> SearchSimilarDocumentsAsync(
            string query,
            int limit = 10,
            float threshold = 0.7f);

        Task<VectorDocument?> GetDocumentByIdAsync(string documentId);
        Task<bool> DeleteDocumentAsync(string documentId);
    }

    // Service principal
    public class FileVectorService : IFileVectorService
    {
        private readonly IMongoCollection<VectorDocument> _collection;
        private readonly MongoVectorDbOptions _options;
        private readonly ILogger<FileVectorService> _logger;
        private readonly ITextExtractionService _textExtractor;
        private readonly IEmbeddingService _embeddingService;

        public FileVectorService(
            IOptions<MongoVectorDbOptions> options,
            ILogger<FileVectorService> logger,
            ITextExtractionService textExtractor,
            IEmbeddingService embeddingService)
        {
            _options = options.Value;
            _logger = logger;
            _textExtractor = textExtractor;
            _embeddingService = embeddingService;

            var client = new MongoClient(_options.ConnectionString);
            var database = client.GetDatabase(_options.DatabaseName);
            _collection = database.GetCollection<VectorDocument>(_options.CollectionName);

            // Créer l'index vectoriel si nécessaire
            CreateVectorIndexIfNotExists().GetAwaiter().GetResult();
        }

        public async Task<string> UploadAndVectorizeFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string documentId,
            string numeroDossier,
            Dictionary<string, object>? additionalMetadata = null)
        {
            try
            {
                _logger.LogInformation("Début de l'upload du fichier: {FileName} pour le dossier: {NumeroDossier}",
                    fileName, numeroDossier);

                // Lire les données binaires du fichier
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                // Extraire le texte du fichier
                var extractedText = await _textExtractor.ExtractTextAsync(fileData, contentType);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new InvalidOperationException($"Impossible d'extraire le texte du fichier {fileName}");
                }

                // Générer l'embedding vectoriel
                var embedding = await _embeddingService.GenerateEmbeddingAsync(extractedText);

                // Créer le document vectoriel
                var vectorDocument = new VectorDocument
                {
                    DocumentId = documentId,
                    NumeroDossier = numeroDossier,
                    FileName = fileName,
                    ContentType = contentType,
                    FileSize = fileData.Length,
                    FileData = fileData,
                    ExtractedText = extractedText,
                    Embedding = embedding,
                    UploadedAt = DateTime.UtcNow,
                    Metadata = additionalMetadata ?? new Dictionary<string, object>()
                };

                // Ajouter les métadonnées système
                vectorDocument.Metadata["original_file_name"] = fileName;
                vectorDocument.Metadata["content_type"] = contentType;
                vectorDocument.Metadata["file_size"] = fileData.Length;

                // Insérer dans MongoDB Atlas
                await _collection.InsertOneAsync(vectorDocument);

                _logger.LogInformation("Fichier {FileName} vectorisé et stocké avec succès. ID: {DocumentId}",
                    fileName, documentId);

                return vectorDocument.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'upload et vectorisation du fichier {FileName}", fileName);
                throw;
            }
        }

        public async Task<List<VectorDocument>> SearchSimilarDocumentsAsync(
            string query,
            int limit = 10,
            float threshold = 0.7f)
        {
            try
            {
                // Générer l'embedding pour la requête
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

                // Pipeline d'agrégation pour la recherche vectorielle
                var pipeline = new[]
                {
                new BsonDocument("$vectorSearch", new BsonDocument
                {
                    { "index", _options.VectorIndexName },
                    { "path", "Embedding" },
                    { "queryVector", new BsonArray(queryEmbedding) },
                    { "numCandidates", limit * 10 },
                    { "limit", limit }
                }),
                new BsonDocument("$addFields", new BsonDocument
                {
                    { "score", new BsonDocument("$meta", "vectorSearchScore") }
                }),
                new BsonDocument("$match", new BsonDocument
                {
                    { "score", new BsonDocument("$gte", threshold) }
                })
            };

                var results = await _collection.Aggregate<VectorDocument>(pipeline).ToListAsync();

                _logger.LogInformation("Recherche vectorielle effectuée. {Count} documents trouvés", results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche vectorielle pour: {Query}", query);
                throw;
            }
        }

        public async Task<VectorDocument?> GetDocumentByIdAsync(string documentId)
        {
            var filter = Builders<VectorDocument>.Filter.Eq(d => d.DocumentId, documentId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            var filter = Builders<VectorDocument>.Filter.Eq(d => d.DocumentId, documentId);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        private async Task CreateVectorIndexIfNotExists()
        {
            try
            {
                var indexKeys = Builders<VectorDocument>.IndexKeys
                    .Ascending(d => d.DocumentId)
                    .Ascending(d => d.NumeroDossier);

                var indexOptions = new CreateIndexOptions
                {
                    Name = "document_metadata_index",
                    Background = true
                };

                await _collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<VectorDocument>(indexKeys, indexOptions));

                // Note: L'index vectoriel doit être créé via MongoDB Atlas UI ou CLI
                // car il nécessite des paramètres spécifiques non supportés par le driver
                _logger.LogInformation("Index de métadonnées créé pour la collection {CollectionName}",
                    _options.CollectionName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de créer l'index (peut-être existe-t-il déjà)");
            }
        }
    }
}
