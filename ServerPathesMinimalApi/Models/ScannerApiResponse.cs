namespace ServerPathsMinimalApi.Models;

public record ScannerApiResponse(int Total, int Limit, int Offset, List<ScannerFileItem>? Data);
public record LinkSyncResult(int Status, string Link, int? Id);
public record ScannerFileItem(string Path, string Name);
