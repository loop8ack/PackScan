using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal abstract class PackageContent<TContent, TType> : IPackageContent<TContent, TType>
    where TContent : class
    where TType : struct, Enum
{
    public IPackagesProviderFile File { get; }
    public TType Type { get; }

    public PackageContent(IPackagesProviderFile file, TType type)
    {
        File = file;
        Type = type;
    }

    public abstract TContent LoadContent();
}
