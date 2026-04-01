//using Microsoft.Extensions.Options;
//using ServerPathsMinimalApi.Models;
//using ServerPathsMinimalApi.Services.Interfaces;
//using System.Collections.Frozen;
//using System.ComponentModel;
//using System.Runtime.InteropServices;

//namespace ServerPathsMinimalApi.Services;

//public sealed class NetworkShareAccess : IDisposable
//{
//    private readonly string _networkName;

//    public NetworkShareAccess(string networkName, string username, string password)
//    {
//        _networkName = networkName;

//        var netResource = new NetResource
//        {
//            Scope = ResourceScope.GlobalNetwork,
//            ResourceType = ResourceType.Disk,
//            DisplayType = ResourceDisplaytype.Share,
//            RemoteName = networkName
//        };

//        var result = WNetAddConnection2(netResource, password, username, 0);

//        if (result != 0)
//        {
//            throw new Win32Exception(result, $"Ошибка подключения к сетевой шаре {_networkName}. Код: {result}");
//        }
//    }

//    public void Dispose()
//    {
//        WNetCancelConnection2(_networkName, 0, true);
//    }

//    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//    private static extern int WNetAddConnection2(NetResource lpNetResource, string lpPassword, string lpUsername, int dwFlags);

//    [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
//    private static extern int WNetCancelConnection2(string lpName, int dwFlags, bool fForce);

//    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
//    private class NetResource
//    {
//        public ResourceScope Scope;
//        public ResourceType ResourceType;
//        public ResourceDisplaytype DisplayType;
//        public int Usage;
//        public string? LocalName;
//        public string RemoteName = string.Empty;
//        public string? Comment;
//        public string? Provider;
//    }

//    private enum ResourceScope : int { GlobalNetwork = 2 }
//    private enum ResourceType : int { Disk = 1 }
//    private enum ResourceDisplaytype : int { Share = 3 }
//}

//using Microsoft.Extensions.Options;
//using ServerPathsMinimalApi.Models;
//using ServerPathsMinimalApi.Services.Interfaces;
//using System.Collections.Frozen;

//namespace ServerPathsMinimalApi.Services;

//public class FileProviderService(ILogger<FileProviderService> logger, IOptions<FileServiceOptions> options) : BackgroundService, IFileProviderService
//{
//    private readonly FileServiceOptions _options = options.Value;
//    public FrozenSet<string> CachedFiles { get; private set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase).ToFrozenSet();

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                // Запускаем в пуле потоков, чтобы не блочить старт приложения
//                CachedFiles = await Task.Run(() => GetScanAndProcess(stoppingToken), stoppingToken);
//                logger.LogInformation("Network scan completed. Cached {Count} files.", CachedFiles.Count);
//            }
//            catch (Exception ex)
//            {
//                logger.LogError(ex, "Network Scan failed");
//            }
//            await Task.Delay(TimeSpan.FromMinutes(_options.RefreshIntervalMinutes), stoppingToken);
//        }
//    }

//    private FrozenSet<string> GetScanAndProcess(CancellationToken ct)
//    {
//        var result = new HashSet<string>(150_000, StringComparer.OrdinalIgnoreCase);
//        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };

//        string networkPath = _options.PicsPath;
//        int prefixLength = networkPath.Length;
//        using (string.IsNullOrEmpty(_options.ShareUsername) ? null : new NetworkShareAccess(networkPath, _options.ShareUsername, _options.SharePassword))
//        {
//            foreach (var file in Directory.EnumerateFiles(networkPath, "*.jpg", options))
//            {
//                if (file.Length <= prefixLength) continue;

//                ct.ThrowIfCancellationRequested();

//                // Защита от кривых слэшей
//                var rel = file.AsSpan(prefixLength).TrimStart('\\').TrimStart('/');

//                result.Add(string.Create(rel.Length, rel, (dest, src) =>
//                {
//                    for (int i = 0; i < src.Length; i++)
//                    {
//                        dest[i] = src[i] == '\\' ? '/' : src[i];
//                    }
//                }));
//            }
//        }

//        return result.ToFrozenSet();
//    }
//}