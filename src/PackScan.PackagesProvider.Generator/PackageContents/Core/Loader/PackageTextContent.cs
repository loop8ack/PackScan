using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageTextContent : PackageContent<string, TextType>
{
    public PackageTextContent(IPackagesProviderFile file, TextType type)
        : base(file, type)
    {
    }

    public override string LoadContent()
        => File.ReadAllText();
}
