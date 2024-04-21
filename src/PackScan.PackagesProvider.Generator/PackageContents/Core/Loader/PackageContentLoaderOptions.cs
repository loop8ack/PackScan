namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageContentLoaderOptions
{
    public required string DownloadCacheFolder { get; init; }
    public required TimeSpan DownloadCacheAccessTimeout { get; init; }
    public required TimeSpan DownloadCacheAccessRetryDelay { get; init; }
}
