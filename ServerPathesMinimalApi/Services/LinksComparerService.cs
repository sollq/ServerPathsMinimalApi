using ServerPathsMinimalApi.Models;
using System.Text.Json;
using System.Collections.Concurrent;

public class LinksComparerService(ILogger<LinksComparerService> logger, IHttpClientFactory httpClientFactory) : ILinksComparerService
{
    private const int ApiLimit = 10;
    private const int MaxDegreeOfParallelism = 8;

    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<List<LinkSyncResult>> GetInvalidLinks(LinksComparisonRequest req, CancellationToken ct = default)
    {
        if (req?.LinksInBd == null || req.LinksInBd.Count == 0)
            return [];

        var groupedDbLinks = req.LinksInBd
            .Where(link => !string.IsNullOrWhiteSpace(link.Url))
            .GroupBy(link => ExtractSearchPrefix(link.Url))
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new ConcurrentBag<LinkSyncResult>();
        using var client = httpClientFactory.CreateClient("ScannerClient");

        var parallelOptions = new ParallelOptions
        {
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CancellationToken = ct
        };

        await Parallel.ForEachAsync(groupedDbLinks, parallelOptions, async (kvp, token) =>
        {
            var baseDir = kvp.Key;
            var expectedDbObjects = kvp.Value;

            var actualLinksInFs = await FetchAllFilesFromApiAsync(client, baseDir, token);

            var expectedUrls = expectedDbObjects
                .Select(x => x.Url)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var dbObj in expectedDbObjects)
            {
                if (!actualLinksInFs.Contains(dbObj.Url))
                {
                    results.Add(new LinkSyncResult(1, dbObj.Url, dbObj.Id));
                }
            }

            var missingInDb = actualLinksInFs.Except(expectedUrls, StringComparer.OrdinalIgnoreCase);
            foreach (var fsLink in missingInDb)
            {
                results.Add(new LinkSyncResult(2, fsLink, 0));
            }
        });

        return [.. results];
    }

    private async Task<HashSet<string>> FetchAllFilesFromApiAsync(HttpClient client, string baseDir, CancellationToken ct)
    {
        var fsPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int offset = 0;
        int total = 0;

        do
        {
            var url = $"/snapshot?path_filter={Uri.EscapeDataString(baseDir)}&limit={ApiLimit}&offset={offset}";
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, url);
                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync(ct);
                var apiResponse = await JsonSerializer.DeserializeAsync<ScannerApiResponse>(stream, _jsonOptions, ct);

                if (apiResponse?.Data == null || apiResponse.Data.Count == 0)
                    break;

                total = apiResponse.Total;

                foreach (var item in apiResponse.Data)
                {
                    var fullPath = string.Concat(item.Path, item.Name);

                    fsPaths.Add(fullPath);
                }

                offset += ApiLimit;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Сбой при запросе к сканеру: {BaseDir}, смещение: {Offset}", baseDir, offset);
                break;
            }

        } while (offset < total && !ct.IsCancellationRequested);

        return fsPaths;
    }

    private static string ExtractSearchPrefix(string fullLink)
    {
        if (string.IsNullOrWhiteSpace(fullLink)) return string.Empty;

        //Находим первый слэш (после 'eu')
        int firstSlash = fullLink.IndexOf('/');
        if (firstSlash >= 0)
        {
            //Находим второй слэш (после 'bmw-2131231')
            int secondSlash = fullLink.IndexOf('/', firstSlash + 1);
            if (secondSlash >= 0)
            {
                //Возвращаем строку включительно со вторым слэшем -> "eu/bmw-2131231/"
                return fullLink[..(secondSlash + 1)];
            }
        }

        //Фолбэк на старую логику, если путь оказался нестандартным
        int lastSlashIndex = fullLink.LastIndexOf('/');
        return lastSlashIndex >= 0 ? fullLink[..(lastSlashIndex + 1)] : string.Empty;
    }
}