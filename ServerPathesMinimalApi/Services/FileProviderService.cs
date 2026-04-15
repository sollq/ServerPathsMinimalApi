using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;
using System.Collections.Frozen;

namespace ServerPathsMinimalApi.Services;

public class FileProviderService(ILogger<FileProviderService> logger, IOptions<FileServiceOptions> options, IHttpClientFactory httpClientFactory) : IFileProviderService
{
    private readonly FileServiceOptions _options = options.Value;
    protected async Task<FrozenSet<string>?> GetAsync(CancellationToken stoppingToken)
    {
        try
        {
            return await GetScanAndProcess(stoppingToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Get files failed");
            return [];
        }
    }

    private async Task<FrozenSet<string>> GetScanAndProcess(CancellationToken ct)
    {
        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("X-API-Key", _options.ExternalApiKey);

        var response = await client.GetAsync($"{_options.ScannerUrl}/report_last", ct);
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<List<FileReportItem>>(cancellationToken: ct);
        if (items == null || items.Count == 0) return [];

        var result = new HashSet<string>(items.Count, StringComparer.OrdinalIgnoreCase);
        int rootLen = _options.PicsPath.Length;

        foreach (var item in items)
        {
            var pSpan = item.Path.AsSpan(rootLen).TrimStart('\\').TrimStart('/');
            var nSpan = item.Name.AsSpan();

            bool hasPath = !pSpan.IsEmpty;
            int totalLength = pSpan.Length + (hasPath ? 1 : 0) + nSpan.Length;

            var finalStr = string.Create(totalLength, (item, rootLen, hasPath), (dest, state) =>
            {
                var p = state.item.Path.AsSpan(state.rootLen).TrimStart('\\').TrimStart('/');
                var n = state.item.Name.AsSpan();

                for (int i = 0; i < p.Length; i++)
                    dest[i] = p[i] == '\\' ? '/' : p[i];

                if (state.hasPath)
                    dest[p.Length] = '/';

                int nameStart = state.hasPath ? p.Length + 1 : 0;
                for (int i = 0; i < n.Length; i++)
                    dest[nameStart + i] = n[i] == '\\' ? '/' : n[i];
            });

            result.Add(finalStr);
        }

        return result.ToFrozenSet();
    }
}