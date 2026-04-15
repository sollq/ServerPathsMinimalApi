using ServerPathsMinimalApi.Models;

public interface ILinksComparerService
{
    public LinksComparisonResponse? GetInvalidLinks(LinksComparisonRequest request);
}