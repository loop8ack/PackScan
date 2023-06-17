using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageImageContent : PackageContent<byte[], ImageType>
{
    public PackageImageContent(IPackagesProviderFile file, ImageType type)
        : base(file, type)
    {
    }

    public override byte[] LoadContent()
        => File.ReadAllBytes();
}
