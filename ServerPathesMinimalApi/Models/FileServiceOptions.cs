namespace ServerPathsMinimalApi.Models
{
    public record FileServiceOptions
    {
        public string ApiKey { get; init; } = string.Empty;
        public int RefreshIntervalMinutes { get; init; } = 10;
        public string PicsPath { get; init; } = string.Empty;
    }
}