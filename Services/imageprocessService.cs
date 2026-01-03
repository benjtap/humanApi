using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp; // For Rgba32
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SelfApiproj.Services
{
    public interface IImagePreprocessingService
    {
        /// <summary>
        /// Améliore une image pour optimiser la précision OCR
        /// </summary>
        /// <param name="imageData">Données binaires de l'image</param>
        /// <returns>Image optimisée pour OCR</returns>
        Task<byte[]> PreprocessForOcrAsync(byte[] imageData);

        /// <summary>
        /// Détecte l'orientation d'une image
        /// </summary>
        /// <param name="imageData">Données binaires de l'image</param>
        /// <returns>Angle de rotation nécessaire</returns>
        Task<int> DetectOrientationAsync(byte[] imageData);
    }

    public class ImagePreprocessingService : IImagePreprocessingService
    {
        private readonly ILogger<ImagePreprocessingService> _logger;

        public ImagePreprocessingService(ILogger<ImagePreprocessingService> logger)
        {
            _logger = logger;
        }

        public async Task<byte[]> PreprocessForOcrAsync(byte[] imageData)
        {
            try
            {
              //  using var image = Image.Load(imageData);
                using Image<Rgba32> image = Image.Load<Rgba32>(imageData);

                _logger.LogDebug("Préprocessing image {Width}x{Height} pour OCR", image.Width, image.Height);

                // 1. Corriger l'orientation basée sur les données EXIF
                image.Mutate(x => x.AutoOrient());

                // 2. Redimensionner si l'image est trop petite (améliore la précision)
                if (image.Width < 300 || image.Height < 300)
                {
                    var scaleFactor = Math.Max(300.0 / image.Width, 300.0 / image.Height);
                    var newWidth = (int)(image.Width * scaleFactor);
                    var newHeight = (int)(image.Height * scaleFactor);

                    image.Mutate(x => x.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                }

                // 3. Convertir en niveaux de gris (améliore souvent la précision OCR)
                image.Mutate(x => x.Grayscale());

                // 4. Améliorer le contraste
                image.Mutate(x => x.Contrast(1.3f));

                // 5. Augmenter légèrement la netteté
                image.Mutate(x => x.GaussianSharpen(0.5f));

                // 6. Appliquer un filtre de débruitage léger
               // image.Mutate(x => x.MedianFilter(1));

                // Sauvegarder le résultat optimisé
                using var outputStream = new MemoryStream();
                await image.SaveAsPngAsync(outputStream); // PNG pour préserver la qualité pour OCR

                var processedData = outputStream.ToArray();

                _logger.LogDebug("Image préprocessée: {OriginalSize} -> {ProcessedSize} bytes",
                    imageData.Length, processedData.Length);

                return processedData;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Échec du préprocessing, utilisation de l'image originale");
                return imageData; // Fallback vers l'image originale
            }
        }

        public async Task<int> DetectOrientationAsync(byte[] imageData)
        {
            try
            {
                using var image = Image.Load(imageData);
                var profile = image.Metadata.ExifProfile;
                // Analyser les données EXIF pour l'orientation
                if (profile != null)
                {
                    //var orientationValue = image.Metadata.ExifProfile.GetValue(SixLabors.ImageSharp.Metadata.Profiles.Exif.ExifTag.Orientation);

                    ushort? Orientation = null; // Declare as nullable string
                    if (profile.TryGetValue(ExifTag.Orientation, out IExifValue<ushort> exifOrientationValue)) // Out parameter needs to be ExifValue<string>
                    {
                        Orientation = exifOrientationValue.Value; // Get the actual string value
                        if (Orientation != null)
                        {
                            return Orientation switch
                            {
                                3 => 180, // Rotation 180°
                                6 => 90,  // Rotation 90° horaire
                                8 => 270, // Rotation 90° anti-horaire
                                _ => 0    // Pas de rotation nécessaire
                            };
                        }
                    }


                    
                }

                // TODO: Implémentation d'une détection automatique d'orientation
                // basée sur l'analyse du contenu de l'image (direction du texte, etc.)

                await Task.CompletedTask;
                return 0; // Pas de rotation détectée
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Impossible de détecter l'orientation de l'image");
                return 0;
            }
        }
    }
}
