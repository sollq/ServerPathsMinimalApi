namespace ServerPathsMinimalApi.Models;

public record ScannerApiResponse(int Total, int Limit, int Offset, List<ScannerFileItem>? Data);
public record LinkSyncResult(int? Id, string Link, int Status);
public record ScannerFileItem(string Path, string Name);
