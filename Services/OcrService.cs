using System.Text.Json;

namespace smart_receipt_api.Services
{
    public class OcrService : IOcrService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<OcrService> _logger;

        public OcrService(HttpClient httpClient, IConfiguration configuration, ILogger<OcrService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<Dictionary<string, string>> ExtractReceiptDataAsync(IFormFile imageFile)
        {
            var result = new Dictionary<string, string>();

            try
            {
                // Azure Vision API endpoint ve key
                var endpoint = _configuration["AzureVision:Endpoint"];
                var apiKey = _configuration["AzureVision:ApiKey"];

                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("Azure Vision API credentials not configured");
                    result["error"] = "OCR service not configured";
                    return result;
                }

                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                var imageBytes = ms.ToArray();

                // Azure Computer Vision Read API - Step 1: Submit image
                var analyzeUrl = $"{endpoint.TrimEnd('/')}/vision/v3.2/read/analyze";

                using var request = new HttpRequestMessage(HttpMethod.Post, analyzeUrl);
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);
                request.Content = new ByteArrayContent(imageBytes);
                request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                var response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError($"Azure OCR API error: {response.StatusCode} - {errorContent}");
                    result["error"] = $"OCR API error: {response.StatusCode}";
                    return result;
                }

                // Step 2: Get the operation location from response headers
                if (!response.Headers.TryGetValues("Operation-Location", out var operationLocations))
                {
                    _logger.LogError("No Operation-Location header in response");
                    result["error"] = "OCR operation failed";
                    return result;
                }

                var operationLocation = operationLocations.First();

                // Step 3: Poll for results
                string rawText = await PollForResultsAsync(operationLocation, apiKey);

                result["rawText"] = rawText;
                result["status"] = "success";

                _logger.LogInformation($"OCR completed. Raw text length: {rawText.Length}");

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError($"OCR Error: {ex.Message}");
                result["error"] = "OCR processing failed";
                return result;
            }
        }

        private async Task<string> PollForResultsAsync(string operationLocation, string apiKey)
        {
            const int maxRetries = 10;
            const int delayMs = 1000;

            for (int i = 0; i < maxRetries; i++)
            {
                await Task.Delay(delayMs);

                using var request = new HttpRequestMessage(HttpMethod.Get, operationLocation);
                request.Headers.Add("Ocp-Apim-Subscription-Key", apiKey);

                var response = await _httpClient.SendAsync(request);
                var jsonResponse = await response.Content.ReadAsStringAsync();

                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusElement))
                {
                    var status = statusElement.GetString();

                    if (status == "succeeded")
                    {
                        return ExtractTextFromReadResult(root);
                    }
                    else if (status == "failed")
                    {
                        _logger.LogError("Azure OCR operation failed");
                        return string.Empty;
                    }
                    // Continue polling if status is "running" or "notStarted"
                }
            }

            _logger.LogWarning("OCR polling timed out");
            return string.Empty;
        }

        private string ExtractTextFromReadResult(JsonElement root)
        {
            var textLines = new List<string>();

            try
            {
                if (root.TryGetProperty("analyzeResult", out var analyzeResult))
                {
                    if (analyzeResult.TryGetProperty("readResults", out var readResults))
                    {
                        foreach (var page in readResults.EnumerateArray())
                        {
                            if (page.TryGetProperty("lines", out var lines))
                            {
                                foreach (var line in lines.EnumerateArray())
                                {
                                    if (line.TryGetProperty("text", out var textElement))
                                    {
                                        textLines.Add(textElement.GetString() ?? string.Empty);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error extracting text from OCR result: {ex.Message}");
            }

            return string.Join("\n", textLines);
        }
    }
}
