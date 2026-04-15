namespace ServerPathsMinimalApi.Models
{
    public record FileServiceOptions
    {
        public string ApiKey { get; init; } = string.Empty;
        public string ExternalApiKey { get; init; } = string.Empty;
        public string PicsPath { get; init; } = string.Empty; 
        public string ScannerUrl { get; init; } = string.Empty;
    }
}