namespace ServerPathsMinimalApi.Models;

public sealed record LinksComparisonResponse(
    IReadOnlyCollection<LinkSyncResult?> OfferShopsWithInvalidLinks
);