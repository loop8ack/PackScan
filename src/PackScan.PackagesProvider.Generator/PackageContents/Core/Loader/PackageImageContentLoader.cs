using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageImageContentLoader : PackageContentLoader<byte[], ImageType>
{
    protected override IReadOnlyDictionary<string, ImageType> MimeTypeTypeMapping => FileExtensionMappings.ImageTypeByMimeType;
    protected override IReadOnlyDictionary<string, ImageType> FileExtensionTypeMapping => FileExtensionMappings.ImageTypeByExtension;

    public PackageImageContentLoader(IPackagesProviderFilesManager filesManager, IHttpClientFactory httpClientFactory, IPackagesProviderFileModification? modification, PackageContentLoaderOptions options)
        : base(filesManager, httpClientFactory, modification, options)
    {
    }

    protected override PackageContent<byte[], ImageType> CreateContent(IPackagesProviderFile file, ImageType type)
        => new PackageImageContent(file, type);
}
