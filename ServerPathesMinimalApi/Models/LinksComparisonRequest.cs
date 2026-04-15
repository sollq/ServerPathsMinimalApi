namespace ServerPathsMinimalApi.Models;

public sealed record LinkItem(int Id, string Url);
public sealed record LinksComparisonRequest(
    string LinkBaseDirectory,
    IReadOnlyCollection<LinkItem> LinksInBd
);