using ServerPathsMinimalApi.Models;

public interface ILinksComparerService
{
    LinksComparisonResponse? GetInvalidLinks(LinksComparisonRequest request);
}