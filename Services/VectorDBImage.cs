using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;

using Webhttp.Models;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats; // For Rgba32


namespace SelfApiproj.Services
{

    // Interface des services
    public interface IImageFileService
    {
        Task<ImageFileResult> SaveImageAsync(IFormFile file, string documentId, string numeroDossier);
        Task<string> GetImageUrlAsync(string relativePath);
        Task<byte[]?> GetImageBytesAsync(string relativePath);
        Task<bool> DeleteImageAsync(string relativePath);
        string GetThumbnailPath(string originalPath);
    }

    // Service de gestion des fichiers locaux
    public class LocalImageFileService : IImageFileService
    {
        private readonly ImageRagOptions _options;
        private readonly ILogger<LocalImageFileService> _logger;

        public LocalImageFileService(IOptions<ImageRagOptions> options, ILogger<LocalImageFileService> logger)
        {
            _options = options.Value;
            _logger = logger;
            EnsureDirectoriesExist();
        }

        public async Task<ImageFileResult> SaveImageAsync(IFormFile file, string documentId, string numeroDossier)
        {
            try
            {
                // Génération du nom de fichier unique
                var fileExtension = ".jpg"; // Toujours JPG en sortie
                var fileName = $"{documentId}_{Guid.NewGuid()}{fileExtension}";
                var yearMonth = DateTime.Now.ToString("yyyy-MM");
                var relativePath = Path.Combine("images", yearMonth, fileName);
                var fullPath = Path.Combine(_options.ImagesBasePath, yearMonth, fileName);
                var thumbnailPath = Path.Combine("thumbnails", yearMonth, fileName);
                var fullThumbnailPath = Path.Combine(_options.ImagesBasePath, "thumbnails", yearMonth, fileName);

                // Création des dossiers si nécessaire
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                Directory.CreateDirectory(Path.GetDirectoryName(fullThumbnailPath)!);

                // Traitement et optimisation de l'image
                using var inputStream = file.OpenReadStream();
              //  using var image = await Image<Rgba32>.LoadAsync(inputStream);

             
                Image<Rgba32> image = await Image.LoadAsync<Rgba32>(inputStream);

                // Optimisation de l'image principale
                var optimizedImage = OptimizeImage(image, _options.JpegQuality);
                await optimizedImage.SaveAsJpegAsync(fullPath, new JpegEncoder { Quality = _options.JpegQuality });

                // Création de la miniature
                var thumbnail = CreateThumbnail(image, 300, 300);
                await thumbnail.SaveAsJpegAsync(fullThumbnailPath, new JpegEncoder { Quality = 80 });

                // Calcul de la taille du fichier
                var fileInfo = new FileInfo(fullPath);

                _logger.LogInformation("Image sauvegardée: {FileName} ({FileSize} bytes)", fileName, fileInfo.Length);

                return new ImageFileResult
                {
                    StoredFileName = fileName,
                    RelativePath = relativePath.Replace('\\', '/'),
                    ThumbnailPath = thumbnailPath.Replace('\\', '/'),
                    FullUrl = $"{_options.BaseUrl}/{relativePath.Replace('\\', '/')}",
                    FileSizeBytes = fileInfo.Length,
                    Width = optimizedImage.Width,
                    Height = optimizedImage.Height
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la sauvegarde de l'image pour le document {DocumentId}", documentId);
                throw new InvalidOperationException("Impossible de sauvegarder l'image", ex);
            }
        }

        public async Task<string> GetImageUrlAsync(string relativePath)
        {
            var fullPath = Path.Combine(_options.ImagesBasePath, relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Image non trouvée");

            return $"{_options.BaseUrl}/{relativePath.Replace('\\', '/')}";
        }

        public async Task<byte[]?> GetImageBytesAsync(string relativePath)
        {
            var fullPath = Path.Combine(_options.ImagesBasePath, relativePath);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<bool> DeleteImageAsync(string relativePath)
        {
            try
            {
                var fullPath = Path.Combine(_options.ImagesBasePath, relativePath);
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);

                    // Suppression de la miniature aussi
                    var thumbnailPath = GetThumbnailPath(relativePath);
                    var fullThumbnailPath = Path.Combine(_options.ImagesBasePath, thumbnailPath);
                    if (File.Exists(fullThumbnailPath))
                        File.Delete(fullThumbnailPath);

                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'image {RelativePath}", relativePath);
                return false;
            }
        }

        public string GetThumbnailPath(string originalPath)
        {
            return originalPath.Replace("images/", "thumbnails/");
        }

        private void EnsureDirectoriesExist()
        {
            var imagesDir = Path.Combine(_options.ImagesBasePath, "images");
            var thumbnailsDir = Path.Combine(_options.ImagesBasePath, "thumbnails");

            Directory.CreateDirectory(imagesDir);
            Directory.CreateDirectory(thumbnailsDir);
        }

        private Image OptimizeImage(Image<Rgba32> originalImage, int quality)
        {
            var optimized = originalImage.Clone();

            // Redimensionnement si l'image est trop grande (max 2048px sur le côté le plus long)
            if (optimized.Width > 2048 || optimized.Height > 2048)
            {
                optimized.Mutate(x => x.Resize(new ResizeOptions
                {
                    Mode = ResizeMode.Max,
                    Size = new Size(2048, 2048)
                }));
            }

            return optimized;
        }

        private Image CreateThumbnail(Image<Rgba32> originalImage, int maxWidth, int maxHeight)
        {
            var thumbnail = originalImage.Clone();
            thumbnail.Mutate(x => x.Resize(new ResizeOptions
            {
                Mode = ResizeMode.Max,
                Size = new Size(maxWidth, maxHeight)
            }));

            return thumbnail;
        }
    }

}
