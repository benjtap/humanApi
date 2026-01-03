using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Webhttp.Models;

namespace SelfApiproj.Services
{

    public interface IImageStorageService
    {
        /// <summary>
        /// Sauvegarde une image sur le système de fichiers ou cloud storage
        /// </summary>
        /// <param name="imageData">Données binaires de l'image</param>
        /// <param name="fileName">Nom du fichier original</param>
        /// <param name="documentId">ID unique du document</param>
        /// <returns>Chemin relatif vers l'image sauvegardée</returns>
        Task<string> SaveImageAsync(byte[] imageData, string fileName, string documentId);

        /// <summary>
        /// Récupère les données d'une image
        /// </summary>
        /// <param name="imagePath">Chemin vers l'image</param>
        /// <returns>Données binaires de l'image</returns>
        Task<byte[]> GetImageDataAsync(string imagePath);

        /// <summary>
        /// Supprime une image du stockage
        /// </summary>
        /// <param name="imagePath">Chemin vers l'image à supprimer</param>
        Task DeleteImageAsync(string imagePath);

        /// <summary>
        /// Génère une miniature d'image
        /// </summary>
        /// <param name="imagePath">Chemin vers l'image source</param>
        /// <param name="size">Taille de la miniature</param>
        /// <returns>Données binaires de la miniature</returns>
        Task<byte[]> GenerateThumbnailAsync(string imagePath, int size = 200);
    }

    public class FileSystemImageStorageService : IImageStorageService
    {
        private readonly MongoVectorDbOptions _options;
        private readonly ILogger<FileSystemImageStorageService> _logger;

        public FileSystemImageStorageService(IOptions<MongoVectorDbOptions> options, ILogger<FileSystemImageStorageService> logger)
        {
            _options = options.Value;
            _logger = logger;

            // Créer le dossier de stockage s'il n'existe pas
            if (!Directory.Exists(_options.ImageStoragePath))
            {
                Directory.CreateDirectory(_options.ImageStoragePath);
                _logger.LogInformation("Dossier de stockage d'images créé: {Path}", _options.ImageStoragePath);
            }
        }

        public async Task<string> SaveImageAsync(byte[] imageData, string fileName, string documentId)
        {
            try
            {
                // Nettoyer le nom de fichier et créer un nom unique
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var safeFileName = $"{SanitizeFileName(documentId)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension}";
                var relativePath = Path.Combine("images", safeFileName);
                var fullPath = Path.Combine(_options.ImageStoragePath, safeFileName);

                // Optimiser l'image avant sauvegarde
                var optimizedImageData = await OptimizeImageAsync(imageData, extension);

                await File.WriteAllBytesAsync(fullPath, optimizedImageData);

                _logger.LogInformation("Image sauvegardée: {Path} ({Size} bytes)", fullPath, optimizedImageData.Length);
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de l'image {FileName}", fileName);
                throw;
            }
        }

        public async Task<byte[]> GetImageDataAsync(string imagePath)
        {
            try
            {
                var fileName = Path.GetFileName(imagePath);
                var fullPath = Path.Combine(_options.ImageStoragePath, fileName);

                if (!File.Exists(fullPath))
                {
                    throw new FileNotFoundException($"Image non trouvée: {imagePath}");
                }

                return await File.ReadAllBytesAsync(fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la lecture de l'image {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task DeleteImageAsync(string imagePath)
        {
            try
            {
                var fileName = Path.GetFileName(imagePath);
                var fullPath = Path.Combine(_options.ImageStoragePath, fileName);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("Image supprimée: {Path}", fullPath);
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'image {ImagePath}", imagePath);
                throw;
            }
        }

        public async Task<byte[]> GenerateThumbnailAsync(string imagePath, int size = 200)
        {
            try
            {
                var imageData = await GetImageDataAsync(imagePath);

                //using Image<Rgba32> image = Image<Rgba32>.Load(imageData);

                using Image<Rgba32> image =  Image.Load<Rgba32>(imageData);

                // Calculer les nouvelles dimensions en gardant le ratio
                var ratio = Math.Min((double)size / image.Width, (double)size / image.Height);
                var newWidth = (int)(image.Width * ratio);
                var newHeight = (int)(image.Height * ratio);


                // Redimensionner avec algorithme de haute qualité
                image.Mutate<Rgba32>(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(newWidth, newHeight),
                    Mode = ResizeMode.Max,
                    Sampler = KnownResamplers.Lanczos3
                }));

                // Convertir en JPEG avec compression optimisée
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = 85,
                   // Subsample = JpegSubsample.Ratio420
                });

                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la génération de miniature pour {ImagePath}", imagePath);
                throw;
            }
        }

        private async Task<byte[]> OptimizeImageAsync(byte[] imageData, string extension)
        {
            try
            {
                // Pour les formats déjà optimisés, retourner tel quel
                if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase) ||
                    extension.Equals(".avif", StringComparison.OrdinalIgnoreCase))
                {
                    return imageData;
                }

                using var image = Image.Load(imageData);

                // Limiter la taille maximale pour économiser l'espace
                const int maxDimension = 2048;
                if (image.Width > maxDimension || image.Height > maxDimension)
                {
                    var ratio = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
                    var newWidth = (int)(image.Width * ratio);
                    var newHeight = (int)(image.Height * ratio);

                    image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                }

                // Sauvegarder en JPEG optimisé
                using var outputStream = new MemoryStream();
                await image.SaveAsJpegAsync(outputStream, new JpegEncoder
                {
                    Quality = 90,
                   // Subsample = JpegSubsample.Ratio420
                });

                return outputStream.ToArray();
            }
            catch
            {
                // En cas d'erreur, retourner l'image originale
                return imageData;
            }
        }

        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Concat(fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }
    }

}
