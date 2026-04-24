using ServerPathsMinimalApi.Models;

public interface ILinksComparerService
{
    public Task<List<LinkSyncResult>> GetInvalidLinks(LinksComparisonRequest request, CancellationToken ct = default);
}