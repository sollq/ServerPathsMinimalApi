using ServerPathsMinimalApi.Models;
using System.Text.Json;
using System.Net.Http.Json;

public class LinksComparerService(ILogger<LinksComparerService> logger, IHttpClientFactory httpClientFactory) : ILinksComparerService
{
    private const int ApiLimit = 1000;
    public async Task<List<LinkSyncResult>> GetInvalidLinks(LinksComparisonRequest req, CancellationToken ct = default)
    {
        
        var results = new List<LinkSyncResult>();
        if (req.LinksInBd.Count == 0) return results;

        var groupedDbLinks = req.LinksInBd
            .Where(link => !string.IsNullOrWhiteSpace(link.Url))
            .GroupBy(
                link => ExtractBaseDirectory(link.Url),
                link => link.Url
            )
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .ToDictionary(
                g => g.Key,
                g => g.ToHashSet(StringComparer.OrdinalIgnoreCase)
            );

        using var client = httpClientFactory.CreateClient("ScannerClient");

        foreach (var (baseDir, expectedLinksInDb) in groupedDbLinks)
        {
            var actualLinksInFs = await FetchAllFilesFromApiAsync(client, baseDir, ct);

            var missingInFs = expectedLinksInDb.Except(actualLinksInFs, StringComparer.OrdinalIgnoreCase);
            foreach (var link in missingInFs)
            {
                results.Add(new LinkSyncResult (1, link, 1));
            }

            var missingInDb = actualLinksInFs.Except(expectedLinksInDb, StringComparer.OrdinalIgnoreCase);
            foreach (var link in missingInDb)
            {
                results.Add(new LinkSyncResult(2, link, 1));
            }
        }
        return results;
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
                var response = await client.GetFromJsonAsync<ScannerApiResponse>(url, ct);

                if (response?.Data == null || response.Data.Count == 0)
                    break;

                total = response.Total;

                foreach (var item in response.Data)
                {
                    var cleanPath = item.Path.Trim('/');
                    var fullPath = string.IsNullOrEmpty(cleanPath)
                        ? item.Name
                        : $"{cleanPath}/{item.Name}";

                    fsPaths.Add(fullPath);
                }

                offset += ApiLimit;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Сбой при запросе к сканеру для папки {BaseDir} на смещении {Offset}", baseDir, offset);
                break;
            }

        } while (offset < total);

        return fsPaths;
    }
    private static string ExtractBaseDirectory(string fullLink)
    {
        var parts = fullLink.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2)
        {
            return $"{parts[0]}/{parts[1]}/";
        }
        return string.Empty;
    }


}