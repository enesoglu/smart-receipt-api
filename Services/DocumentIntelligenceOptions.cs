namespace smart_receipt_api.Services
{
    public class DocumentIntelligenceOptions
    {
        public string Endpoint { get; set; } = string.Empty;
        public string ApiKey { get; set; } = string.Empty;
        public string ModelId { get; set; } = "prebuilt-receipt";
    }
}
