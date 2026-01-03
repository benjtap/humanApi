using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Webhttp.Models;

namespace SelfApiproj.Services
{
   
    public interface IOcrService
    {
      
        Task<string> ExtractTextFromImageAsync(byte[] imageData);
    }

    // <summary>
    /// Service OCR utilisant Azure Computer Vision
    /// </summary>
    public class AzureOcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly string _endpoint;
        private readonly string _subscriptionKey;
        private readonly ILogger<AzureOcrService> _logger;
        private readonly IImagePreprocessingService _preprocessingService;

        public AzureOcrService(
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<AzureOcrService> logger,
            IImagePreprocessingService preprocessingService)
        {
            _httpClient = httpClient;
            _endpoint = configuration["AzureComputerVision:Endpoint"] ??
                throw new InvalidOperationException("Azure Computer Vision endpoint manquant");
            _subscriptionKey = configuration["AzureComputerVision:SubscriptionKey"] ??
                throw new InvalidOperationException("Azure Computer Vision key manquante");
            _logger = logger;
            _preprocessingService = preprocessingService;

            _httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);
            _httpClient.Timeout = TimeSpan.FromMinutes(2);
        }

        public async Task<string> ExtractTextFromImageAsync(byte[] imageData)
        {
            try
            {
                _logger.LogInformation("Début OCR Azure pour image de {Size} bytes", imageData.Length);

                // Préprocesser l'image pour améliorer la précision OCR
                var preprocessedImageData = await _preprocessingService.PreprocessForOcrAsync(imageData);

                var requestUri = $"{_endpoint}/vision/v4/read/analyze"; //

                using var content = new ByteArrayContent(preprocessedImageData);
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                // Initier l'analyse OCR
                var response = await _httpClient.PostAsync(requestUri, content);
                response.EnsureSuccessStatusCode();

                // Récupérer l'URL d'opération pour le suivi
                var operationLocation = response.Headers.GetValues("Operation-Location").FirstOrDefault();
                if (string.IsNullOrEmpty(operationLocation))
                {
                    throw new InvalidOperationException("Operation-Location header manquant dans la réponse Azure");
                }

                // Attendre la fin de l'analyse avec retry pattern
                string result = await PollForResultAsync(operationLocation);

                // Extraire le texte du résultat JSON
                var extractedText = ExtractTextFromAzureResponse(result);

                _logger.LogInformation("OCR Azure réalisé avec succès. {CharCount} caractères extraits", extractedText.Length);

                return extractedText;
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Erreur HTTP lors de l'OCR Azure");
                throw new InvalidOperationException("Service OCR Azure temporairement indisponible", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'OCR Azure de l'image");
                throw;
            }
        }

        private async Task<string> PollForResultAsync(string operationLocation)
        {
            const int maxAttempts = 30;
            const int delayMs = 1000;

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                await Task.Delay(delayMs);

                var resultResponse = await _httpClient.GetAsync(operationLocation);
                resultResponse.EnsureSuccessStatusCode();

                var result = await resultResponse.Content.ReadAsStringAsync();

                // Vérifier si l'analyse est terminée
                if (!result.Contains("\"status\":\"running\""))
                {
                    if (result.Contains("\"status\":\"succeeded\""))
                    {
                        return result;
                    }
                    else if (result.Contains("\"status\":\"failed\""))
                    {
                        throw new InvalidOperationException("L'analyse OCR Azure a échoué");
                    }
                }
            }

            throw new TimeoutException("L'analyse OCR Azure a pris trop de temps");
        }

        private string ExtractTextFromAzureResponse(string jsonResult)
        {
            try
            {
                using var document = System.Text.Json.JsonDocument.Parse(jsonResult);
                var root = document.RootElement;

                if (root.TryGetProperty("analyzeResult", out var analyzeResult) &&
                    analyzeResult.TryGetProperty("readResults", out var readResults))
                {
                    var textBuilder = new StringBuilder();

                    foreach (var readResult in readResults.EnumerateArray())
                    {
                        if (readResult.TryGetProperty("lines", out var lines))
                        {
                            foreach (var line in lines.EnumerateArray())
                            {
                                if (line.TryGetProperty("text", out var text))
                                {
                                    textBuilder.AppendLine(text.GetString());
                                }
                            }
                        }
                    }

                    return textBuilder.ToString().Trim();
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'extraction du texte depuis la réponse Azure OCR");
                return string.Empty;
            }
        }
    }



}
