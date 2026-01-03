using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Metadata;

using SixLabors.ImageSharp.Metadata.Profiles.Exif; // For ExifTag enum and other EXIF-specific classes
using SixLabors.ImageSharp.PixelFormats;


using Webhttp.Models;
using ImageMetadata = Webhttp.Models.ImageMetadata;

namespace SelfApiproj.Services
{

    public interface IImageVectorService
    {
        Task<string> UploadAndProcessImageAsync(
             Stream imageStream,
             string fileName,
             string contentType,
             string documentId,
             string numeroDossier,
             Dictionary<string, object>? additionalMetadata = null);

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
        Task<List<VectorDocument>> SearchSimilarImagesAsync(string query, int limit = 10, float threshold = 0.7f);

        Task<VectorDocument?> GetDocumentByIdAsync(string documentId);

        Task<VectorDocument?> GetImageByIdAsync(string documentId);

        Task<byte[]> GetImageDataAsync(string imagePath);

        Task<bool> DeleteDocumentAsync(string documentId);

    

    }

    public class ImageVectorService : IImageVectorService
    {
        private readonly IMongoCollection<VectorDocument> _collection;
        private readonly MongoVectorDbOptions _options;
        private readonly ILogger<ImageVectorService> _logger;
        private readonly ITextExtractionService _textExtractor;
        private readonly IEmbeddingService _embeddingService;
        private readonly IOcrService _ocrService;
        private readonly IImageStorageService _imageStorage;

        public ImageVectorService(
            IOptions<MongoVectorDbOptions> options,
            ILogger<ImageVectorService> logger,
            ITextExtractionService textExtractor,
            IEmbeddingService embeddingService,
            IOcrService ocrService,
            IImageStorageService imageStorage)
        {
            _options = options.Value;
            _logger = logger;
            _textExtractor = textExtractor;
            _embeddingService = embeddingService;
            _ocrService = ocrService;
            _imageStorage = imageStorage;

            var client = new MongoClient(_options.ConnectionString);
            var database = client.GetDatabase(_options.DatabaseName);
            _collection = database.GetCollection<VectorDocument>(_options.CollectionName);

            CreateVectorIndexIfNotExists().GetAwaiter().GetResult();
        }

        public async Task<string> UploadAndProcessImageAsync(
            Stream imageStream,
            string fileName,
            string contentType,
            string documentId,
            string numeroDossier,
            Dictionary<string, object>? additionalMetadata = null)
        {
            try
            {
                _logger.LogInformation("Début du traitement de l'image: {FileName} pour le dossier: {NumeroDossier}",
                    fileName, numeroDossier);

                // Lire les données de l'image
                using var memoryStream = new MemoryStream();
                await imageStream.CopyToAsync(memoryStream);
                var imageData = memoryStream.ToArray();

                // Sauvegarder l'image physiquement
                var imagePath = await _imageStorage.SaveImageAsync(imageData, fileName, documentId);
                var imageUrl = $"{_options.ImageBaseUrl}/{documentId}";

                // Extraire les métadonnées de l'image
                var imageMetadata = await ExtractImageMetadataAsync(imageData);

                // Effectuer l'OCR sur l'image
                var ocrText = await _ocrService.ExtractTextFromImageAsync(imageData);

                // Générer l'embedding basé sur le texte OCR
                var embedding = await _embeddingService.GenerateEmbeddingAsync(ocrText);

                // Créer le document vectoriel
                var vectorDocument = new VectorDocument
                {
                    DocumentId = documentId,
                    NumeroDossier = numeroDossier,
                    FileName = fileName,
                    ContentType = contentType,
                    FileSize = imageData.Length,
                    ImagePath = imagePath,
                    ImageUrl = imageUrl,
                    OcrText = ocrText,
                    ImageMetadata = imageMetadata,
                    ExtractedText = ocrText, // Pour compatibilité avec les recherches existantes
                    Embedding = embedding,
                    UploadedAt = DateTime.UtcNow,
                    Type = DocumentType.Image,
                    Metadata = additionalMetadata ?? new Dictionary<string, object>()
                };

                // Ajouter les métadonnées système
                vectorDocument.Metadata["original_file_name"] = fileName;
                vectorDocument.Metadata["content_type"] = contentType;
                vectorDocument.Metadata["file_size"] = imageData.Length;
                vectorDocument.Metadata["image_width"] = imageMetadata.Width;
                vectorDocument.Metadata["image_height"] = imageMetadata.Height;
                vectorDocument.Metadata["image_format"] = imageMetadata.Format;

                // Insérer dans MongoDB Atlas
                await _collection.InsertOneAsync(vectorDocument);

                _logger.LogInformation("Image {FileName} traitée et stockée avec succès. ID: {DocumentId}, OCR: {OcrLength} caractères",
                    fileName, documentId, ocrText.Length);

                return vectorDocument.Id.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du traitement de l'image {FileName}", fileName);
                throw;
            }
        }

        public async Task<string> UploadAndVectorizeFileAsync(
            Stream fileStream,
            string fileName,
            string contentType,
            string documentId,
            string numeroDossier,
            Dictionary<string, object>? additionalMetadata = null)
        {
            // Vérifier si c'est une image
            if (IsImageContentType(contentType))
            {
                return await UploadAndProcessImageAsync(fileStream, fileName, contentType,
                    documentId, numeroDossier, additionalMetadata);
            }

            // Traitement des documents texte existant
            try
            {
                _logger.LogInformation("Début de l'upload du fichier: {FileName} pour le dossier: {NumeroDossier}",
                    fileName, numeroDossier);

                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                var fileData = memoryStream.ToArray();

                var extractedText = await _textExtractor.ExtractTextAsync(fileData, contentType);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    throw new InvalidOperationException($"Impossible d'extraire le texte du fichier {fileName}");
                }

                var embedding = await _embeddingService.GenerateEmbeddingAsync(extractedText);

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
                    Type = DocumentType.Document,
                    Metadata = additionalMetadata ?? new Dictionary<string, object>()
                };

                vectorDocument.Metadata["original_file_name"] = fileName;
                vectorDocument.Metadata["content_type"] = contentType;
                vectorDocument.Metadata["file_size"] = fileData.Length;

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

        public async Task<List<RetrievedDocument>> SearchSimilarDocumentsAsync(
     double[] queryEmbedding,
     string numeroDossier = null,
     string documentId = null,
     int limit = 5,
     double minScore = 0.7)
        {
            // Construction du pipeline d'agrégation avec $vectorSearch
            var pipeline = new List<BsonDocument>();

            // Stage $vectorSearch avec filtres optionnels
            var vectorSearchStage = new BsonDocument("$vectorSearch", new BsonDocument
        {
            { "index", _settings.VectorIndexName },
            { "path", "embedding" },
            { "queryVector", new BsonArray(queryEmbedding) },
            { "numCandidates", limit * 10 },
            { "limit", limit }
        });

            // Ajout des filtres sur les métadonnées
            if (!string.IsNullOrEmpty(numeroDossier) || !string.IsNullOrEmpty(documentId))
            {
                var filterConditions = new BsonDocument();

                if (!string.IsNullOrEmpty(numeroDossier))
                {
                    filterConditions.Add("metadata.numero_dossier", new BsonDocument("$eq", numeroDossier));
                }

                if (!string.IsNullOrEmpty(documentId))
                {
                    filterConditions.Add("metadata.document_id", new BsonDocument("$eq", documentId));
                }

                // Combinaison des filtres avec $and si plusieurs conditions
                if (filterConditions.ElementCount > 1)
                {
                    vectorSearchStage["$vectorSearch"]["filter"] = new BsonDocument("$and",
                        new BsonArray(filterConditions.Elements.Select(e => new BsonDocument(e.Name, e.Value))));
                }
                else
                {
                    vectorSearchStage["$vectorSearch"]["filter"] = filterConditions;
                }
            }

            pipeline.Add(vectorSearchStage);

            // Stage $project pour inclure le score et les champs nécessaires
            pipeline.Add(new BsonDocument("$project", new BsonDocument
        {
            { "_id", 1 },
            { "content", 1 },
            { "title", 1 },
            { "metadata", 1 },
            { "score", new BsonDocument("$meta", "vectorSearchScore") }
        }));

            // Stage $match pour filtrer par score minimum
            pipeline.Add(new BsonDocument("$match", new BsonDocument
        {
            { "score", new BsonDocument("$gte", minScore) }
        }));

            var results = await _collection.Aggregate<BsonDocument>(pipeline).ToListAsync();

            return results.Select(doc => new RetrievedDocument
            {
                Id = doc["_id"].AsString,
                Content = doc["content"].AsString,
                Title = doc["title"].AsString,
                Metadata = BsonSerializer.Deserialize<DocumentMetadata>(doc["metadata"].AsBsonDocument),
                Score = doc["score"].AsDouble
            }).ToList();
        }




        public async Task<List<VectorDocument>> SearchSimilarDocumentsAsync(
            string query,
            int limit = 10,
            float threshold = 0.7f)
        {
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

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

        public async Task<List<VectorDocument>> SearchSimilarImagesAsync(string query, int limit = 10, float threshold = 0.7f)
        {
            try
            {
                var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);

                var pipeline = new[]
                {
                new BsonDocument("$match", new BsonDocument
                {
                    { "Type", (int)DocumentType.Image }
                }),
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

                _logger.LogInformation("Recherche d'images effectuée. {Count} images trouvées", results.Count);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche d'images pour: {Query}", query);
                throw;
            }
        }

        public async Task<VectorDocument?> GetDocumentByIdAsync(string documentId)
        {
            var filter = Builders<VectorDocument>.Filter.Eq(d => d.DocumentId, documentId);
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<VectorDocument?> GetImageByIdAsync(string documentId)
        {
            var filter = Builders<VectorDocument>.Filter.And(
                Builders<VectorDocument>.Filter.Eq(d => d.DocumentId, documentId),
                Builders<VectorDocument>.Filter.Eq(d => d.Type, DocumentType.Image)
            );
            return await _collection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<byte[]> GetImageDataAsync(string imagePath)
        {
            return await _imageStorage.GetImageDataAsync(imagePath);
        }

        public async Task<bool> DeleteDocumentAsync(string documentId)
        {
            var document = await GetDocumentByIdAsync(documentId);
            if (document == null) return false;

            // Supprimer l'image physique si c'est une image
            if (document.Type == DocumentType.Image && !string.IsNullOrEmpty(document.ImagePath))
            {
                await _imageStorage.DeleteImageAsync(document.ImagePath);
            }

            var filter = Builders<VectorDocument>.Filter.Eq(d => d.DocumentId, documentId);
            var result = await _collection.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        private async Task<ImageMetadata> ExtractImageMetadataAsync(byte[] imageData)
        {
            //using var image = Image.Load(imageData);
            using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

            var metadata = new Webhttp.Models.ImageMetadata
            {
                Width = image.Width,
                Height = image.Height,
                Format = image.Metadata.DecodedImageFormat?.Name,
                FileSpaceUsed = Math.Round(imageData.Length / (1024.0 * 1024.0), 2)
            };

            // Extraction des données EXIF
            if (image.Metadata.ExifProfile != null)
            {
                var profile = image.Metadata.ExifProfile;

                string? cameraMake = null; // Declare as nullable string
                if (profile.TryGetValue(ExifTag.Make, out IExifValue<string> exifMakeValue)) // Out parameter needs to be ExifValue<string>
                {
                    cameraMake = exifMakeValue.Value; // Get the actual string value
                    metadata.CameraMake = cameraMake;


                }

                string? CameraModel = null; // Declare as nullable string
                if (profile.TryGetValue(ExifTag.Model, out IExifValue<string> exifModelValue)) // Out parameter needs to be ExifValue<string>
                {
                    CameraModel = exifMakeValue.Value; // Get the actual string value
                    metadata.CameraModel = cameraMake;


                }

                ushort? Orientation = null; // Declare as nullable string
                if (profile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort> exifOrientationValue)) // Out parameter needs to be ExifValue<string>
                {
                    Orientation = exifOrientationValue.Value; // Get the actual string value
                    metadata.Orientation = Orientation;
                }


                string? dateTimeValue = null; // Declare as nullable string
                if (profile.TryGetValue(ExifTag.DateTime, out IExifValue<string> exifdateTimeValue)) // Out parameter needs to be ExifValue<string>
                {
                    dateTimeValue = exifdateTimeValue.Value; // Get the actual string value
                    if (!string.IsNullOrEmpty(dateTimeValue) && DateTime.TryParse(dateTimeValue, out var dateTime))
                {
                    metadata.DateTaken = dateTime;
                }
                    
                }



                Rational[] gpsLat = null;
                if (profile.TryGetValue(ExifTag.GPSLatitude, out IExifValue<Rational[]> exifLatitude)) // Out parameter needs to be ExifValue<string>
                {
                    gpsLat = exifLatitude.Value; // Get the actual string value
                  
                }

                Rational[] gpsLon = null;
                if (profile.TryGetValue(ExifTag.GPSLongitude, out IExifValue<Rational[]> exifLongitude)) // Out parameter needs to be ExifValue<string>
                {
                    gpsLon = exifLongitude.Value; // Get the actual string value

                }

                // Géolocalisation GPS si disponible
                //var gpsLat = profile.GetValue(ExifTag.GPSLatitude)?.Value;
                //var gpsLon = profile.GetValue(ExifTag.GPSLongitude)?.Value;
                if (gpsLat != null && gpsLon != null)
                {
                    metadata.Location = $"{gpsLat}, {gpsLon}";
                }
            }

            return metadata;
        }

        private static bool IsImageContentType(string contentType)
        {
            return contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }

        private async Task CreateVectorIndexIfNotExists()
        {
            try
            {
                var indexKeys = Builders<VectorDocument>.IndexKeys
                    .Ascending(d => d.DocumentId)
                    .Ascending(d => d.NumeroDossier)
                    .Ascending(d => d.Type);

                var indexOptions = new CreateIndexOptions
                {
                    Name = "document_metadata_index",
                    Background = true
                };

                await _collection.Indexes.CreateOneAsync(
                    new CreateIndexModel<VectorDocument>(indexKeys, indexOptions));

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
