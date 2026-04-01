using Microsoft.Extensions.Options;
using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;

public class LinksComparerService(ILogger<LinksComparerService> logger, IFileProviderService service) : ILinksComparerService
{
    public LinksComparisonResponse? GetInvalidLinks(LinksComparisonRequest request)
    {
        try
        {
            var serverLinks = service.CachedFiles;
            var lookup = serverLinks.GetAlternateLookup<ReadOnlySpan<char>>();
            var baseLen = request.LinkBaseDirectory.Length;
            List<int> intOfferShops = new(request.LinksInBd.Count / 10);
            foreach (var (id, link) in request.LinksInBd)
            {
                if (link.Length <= baseLen || !link.StartsWith(request.LinkBaseDirectory, StringComparison.OrdinalIgnoreCase))
                {
                    intOfferShops.Add(id);
                    continue;
                }

                var relativeSpan = link.AsSpan(baseLen);
                if (!lookup.Contains(relativeSpan))
                {
                    intOfferShops.Add(id);
                }
            }
            return new LinksComparisonResponse(intOfferShops);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Links Comparison failed");
        }
        return null;
    }
}
