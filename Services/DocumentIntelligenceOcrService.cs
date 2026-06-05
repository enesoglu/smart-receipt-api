using Azure;
using Azure.AI.DocumentIntelligence;
using Microsoft.Extensions.Options;
using smart_receipt_api.DTOs;

namespace smart_receipt_api.Services
{
    public class DocumentIntelligenceOcrService : IOcrService
    {
        private const decimal FieldConfidenceThreshold = 0.5m;

        private readonly DocumentIntelligenceClient? _client;
        private readonly DocumentIntelligenceOptions _options;
        private readonly IReceiptService _fallback;
        private readonly ILogger<DocumentIntelligenceOcrService> _logger;

        public DocumentIntelligenceOcrService(
            IOptions<DocumentIntelligenceOptions> options,
            IReceiptService fallback,
            ILogger<DocumentIntelligenceOcrService> logger)
        {
            _options = options.Value;
            _fallback = fallback;
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(_options.Endpoint) && !string.IsNullOrWhiteSpace(_options.ApiKey))
            {
                _client = new DocumentIntelligenceClient(
                    new Uri(_options.Endpoint),
                    new AzureKeyCredential(_options.ApiKey));
            }
        }

        public async Task<ScanResultDto> ScanReceiptAsync(IFormFile imageFile)
        {
            if (_client == null)
            {
                _logger.LogWarning("Document Intelligence is not configured; returning empty result.");
                return new ScanResultDto { RawText = string.Empty };
            }

            using var stream = new MemoryStream();
            await imageFile.CopyToAsync(stream);
            var content = BinaryData.FromBytes(stream.ToArray());

            try
            {
                var options = new AnalyzeDocumentOptions(_options.ModelId, content);
                var operation = await _client.AnalyzeDocumentAsync(WaitUntil.Completed, options);

                var result = operation.Value;
                var rawText = result.Content ?? string.Empty;

                var doc = result.Documents.FirstOrDefault();
                if (doc == null)
                {
                    _logger.LogInformation("Document Intelligence returned no receipt; using regex fallback.");
                    var fallback = _fallback.BuildScanResult(rawText);
                    fallback.RawText = rawText;
                    return fallback;
                }

                return MapToScanResult(rawText, doc);
            }
            catch (RequestFailedException ex)
            {
                _logger.LogError(ex, "Document Intelligence request failed.");
                return new ScanResultDto { RawText = string.Empty };
            }
        }

        private static ScanResultDto MapToScanResult(string rawText, AnalyzedDocument doc)
        {
            return new ScanResultDto
            {
                RawText = rawText,
                StoreName = ReadString(doc, "MerchantName"),
                Date = ReadDate(doc, "TransactionDate"),
                TotalAmount = ReadCurrency(doc, "Total"),
                Items = ReadItems(doc)
            };
        }

        private static string? ReadString(AnalyzedDocument doc, string fieldName)
        {
            if (!doc.Fields.TryGetValue(fieldName, out var field)) return null;
            if (field.Confidence is float c && c < (float)FieldConfidenceThreshold) return null;
            return field.ValueString?.Trim();
        }

        private static DateTime? ReadDate(AnalyzedDocument doc, string fieldName)
        {
            if (!doc.Fields.TryGetValue(fieldName, out var field)) return null;
            if (field.Confidence is float c && c < (float)FieldConfidenceThreshold) return null;
            return field.ValueDate?.DateTime;
        }

        private static decimal? ReadCurrency(AnalyzedDocument doc, string fieldName)
        {
            if (!doc.Fields.TryGetValue(fieldName, out var field)) return null;
            if (field.Confidence is float c && c < (float)FieldConfidenceThreshold) return null;
            return field.ValueCurrency is { } currency ? (decimal)currency.Amount : null;
        }

        private static List<ReceiptItemDto> ReadItems(AnalyzedDocument doc)
        {
            var list = new List<ReceiptItemDto>();
            if (!doc.Fields.TryGetValue("Items", out var itemsField) || itemsField.ValueList == null)
                return list;

            foreach (var item in itemsField.ValueList)
            {
                if (item.ValueDictionary == null) continue;
                var fields = item.ValueDictionary;

                var name = TryGetString(fields, "Description");
                var quantity = TryGetDouble(fields, "Quantity");
                var unitPrice = TryGetCurrency(fields, "Price");
                var totalPrice = TryGetCurrency(fields, "TotalPrice");

                if (string.IsNullOrWhiteSpace(name)) continue;

                var resolvedQuantity = quantity ?? 1m;
                var resolvedTotalPrice = totalPrice ?? (unitPrice * resolvedQuantity) ?? 0m;
                var resolvedUnitPrice = unitPrice ?? (totalPrice / (resolvedQuantity > 0 ? resolvedQuantity : 1m)) ?? 0m;

                if (resolvedTotalPrice <= 0) continue;

                list.Add(new ReceiptItemDto
                {
                    ItemName = name.Trim(),
                    Quantity = resolvedQuantity,
                    UnitPrice = resolvedUnitPrice,
                    Price = resolvedTotalPrice
                });
            }

            return list;
        }

        private static string? TryGetString(DocumentFieldDictionary fields, string key) =>
            fields.TryGetValue(key, out var field) ? field.ValueString?.Trim() : null;

        private static decimal? TryGetDouble(DocumentFieldDictionary fields, string key) =>
            fields.TryGetValue(key, out var field) && field.ValueDouble.HasValue
                ? (decimal)field.ValueDouble.Value
                : null;

        private static decimal? TryGetCurrency(DocumentFieldDictionary fields, string key) =>
            fields.TryGetValue(key, out var field) && field.ValueCurrency is { } currency
                ? (decimal)currency.Amount
                : null;
    }
}
