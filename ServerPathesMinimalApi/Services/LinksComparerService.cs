using ServerPathsMinimalApi.Models;
using ServerPathsMinimalApi.Services.Interfaces;

public class LinksComparerService(ILogger<LinksComparerService> logger, IFileProviderBgService fileProvider) : ILinksComparerService
{
    public LinksComparisonResponse? GetInvalidLinks(LinksComparisonRequest request)
    {
        try
        {
            var serverLinks = fileProvider.GetCurrentFiles();

            if (serverLinks.Count == 0)
            {
                logger.LogWarning("Попытка сравнения ссылок при пустом кеше.");
                return null;
            }

            var lookup = serverLinks.GetAlternateLookup<ReadOnlySpan<char>>();
            var baseLen = request.LinkBaseDirectory?.Length ?? 0;

            var invalidLinkIds = new List<int>(request.LinksInBd.Count / 10);

            foreach (var (id, link) in request.LinksInBd)
            {
                if (string.IsNullOrEmpty(link) ||
                    link.Length <= baseLen ||
                    !link.StartsWith(request.LinkBaseDirectory!, StringComparison.OrdinalIgnoreCase))
                {
                    invalidLinkIds.Add(id);
                    continue;
                }

                var relativeSpan = link.AsSpan(baseLen).TrimStart('/');

                if (!lookup.Contains(relativeSpan))
                {
                    invalidLinkIds.Add(id);
                }
            }

            return new LinksComparisonResponse(invalidLinkIds);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка при сравнении ссылок.");
            return null;
        }
    }
}