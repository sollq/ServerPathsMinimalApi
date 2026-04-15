using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;
using System.Collections.Frozen;
using System.Text.Json;

public class FileCacheBackgroundService(
    ILogger<FileCacheBackgroundService> logger,
    IOptions<FileServiceOptions> options,
    IHttpClientFactory httpClientFactory) : BackgroundService, IFileProviderBgService
{
    private readonly FileServiceOptions _options = options.Value;
    private volatile FrozenSet<string> _cachedFiles = [];
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };
    public FrozenSet<string> GetCurrentFiles() => _cachedFiles;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Yield();

        await RefreshCacheAsync(stoppingToken);
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.RefreshIntervalMinutes));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RefreshCacheAsync(stoppingToken);
        }
    }

    private async Task RefreshCacheAsync(CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Запуск фонового обновления списка файлов...");

            using var client = httpClientFactory.CreateClient("ScannerClient");
            using var stream = await client.GetStreamAsync("/report_last", ct);

            var items = JsonSerializer.DeserializeAsyncEnumerable<FileReportItem>(
                stream,
                _jsonOptions,
                ct);

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            await foreach (var item in items)
            {
                if (item == null || string.IsNullOrEmpty(item.Path) || string.IsNullOrEmpty(item.Name))
                    continue;

                if (!item.Path.StartsWith(_options.PicsPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var relativePathSpan = item.Path.AsSpan(_options.PicsPath.Length).TrimStart('\\').TrimStart('/');
                var nameSpan = item.Name.AsSpan();

                int pathLen = relativePathSpan.Length;
                int nameLen = nameSpan.Length;

                bool needsSeparator = pathLen > 0;
                int totalLen = pathLen + (needsSeparator ? 1 : 0) + nameLen;

                var normalizedPath = string.Create(totalLen,
                    (item.Path, item.Name, Offset: _options.PicsPath.Length, NeedsSeparator: needsSeparator),
                    (dest, state) =>
                    {
                        var pSpan = state.Path.AsSpan(state.Offset).TrimStart('\\').TrimStart('/');
                        var nSpan = state.Name.AsSpan();

                        for (int i = 0; i < pSpan.Length; i++)
                        {
                            dest[i] = pSpan[i] == '\\' ? '/' : pSpan[i];
                        }

                        int currentPos = pSpan.Length;

                        if (state.NeedsSeparator)
                        {
                            dest[currentPos++] = '/';
                        }

                        for (int i = 0; i < nSpan.Length; i++)
                        {
                            dest[currentPos + i] = nSpan[i] == '\\' ? '/' : nSpan[i];
                        }
                    });

                result.Add(normalizedPath);
            }

            if (result.Count == 0)
            {
                logger.LogWarning("Сканер вернул пустой список. Кеш не обновлен.");
                return;
            }

            _cachedFiles = result.ToFrozenSet();
            logger.LogInformation("Кеш файлов успешно обновлен. Элементов: {Count}", _cachedFiles.Count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при фоновом обновлении кеша файлов. Используются старые данные.");
        }
    }
}