using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;
using System.Collections.Frozen;

namespace ServerPathsMinimalApi.Services;

public class FileProviderService(ILogger<FileProviderService> logger, IOptions<FileServiceOptions> options) : BackgroundService, IFileProviderService
{
    private readonly FileServiceOptions _options = options.Value;
    public FrozenSet<string> CachedFiles { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase).ToFrozenSet();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CachedFiles = await Task.Run(() => GetScanAndProcess(stoppingToken), stoppingToken);
                logger.LogInformation($"Network scan completed. Cached {CachedFiles.Count} files.", CachedFiles.Count);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Network Scan failed");
            }
            await Task.Delay(TimeSpan.FromMinutes(_options.RefreshIntervalMinutes), stoppingToken);
        }
    }

    private FrozenSet<string> GetScanAndProcess(CancellationToken ct)
    {
        var result = new HashSet<string>(150_000, StringComparer.OrdinalIgnoreCase);
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

        string networkPath = _options.PicsPath;
        int prefixLength = networkPath.Length;

        foreach (var file in Directory.EnumerateFiles(networkPath, "*.jpg", options))
        {
            if (file.Length <= prefixLength) continue;

            ct.ThrowIfCancellationRequested();

            var rel = file.AsSpan(prefixLength).TrimStart('\\').TrimStart('/');

            result.Add(string.Create(rel.Length, rel, (dest, src) =>
            {
                for (int i = 0; i < src.Length; i++)
                {
                    dest[i] = src[i] == '\\' ? '/' : src[i];
                }
            }));
        }
        
        return result.ToFrozenSet();
    }
}