namespace ServerPathsMinimalApi.Models;

public sealed record LinksComparisonResponse(
    IReadOnlyCollection<int> OfferShopsWithInvalidLinks
);