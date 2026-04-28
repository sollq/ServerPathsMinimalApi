namespace ServerPathsMinimalApi.Models;

public sealed record LinkItem(int Id, string Url);
public sealed record LinksComparisonRequest(
    IReadOnlyCollection<LinkItem> LinksInBd
);