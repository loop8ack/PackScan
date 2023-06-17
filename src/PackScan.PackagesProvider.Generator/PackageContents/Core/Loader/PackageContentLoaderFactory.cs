using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageContentLoaderFactory : IPackageContentLoaderFactory
{
    private readonly IPackagesProviderFilesManager _filesManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _downloadCacheFolder;

    public PackageContentLoaderFactory(IPackagesProviderFilesManager filesManager, IHttpClientFactory httpClientFactory, string downloadCacheFolder)
    {
        _filesManager = filesManager;
        _httpClientFactory = httpClientFactory;
        _downloadCacheFolder = downloadCacheFolder;
    }

    public IPackageContentLoader<byte[], ImageType> CreateImageLoader() => new PackageImageContentLoader(_filesManager, _httpClientFactory, _downloadCacheFolder);
    public IPackageContentLoader<string, TextType> CreateTextLoader() => new PackageTextContentLoader(_filesManager, _httpClientFactory, _downloadCacheFolder);
}
