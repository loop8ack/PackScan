using PackScan.PackagesProvider.Generator.Files;

namespace PackScan.PackagesProvider.Generator.PackageContents;

internal interface IPackageContent
{
    IPackagesProviderFile File { get; }
}

internal interface IPackageContent<TType> : IPackageContent
    where TType : struct, Enum
{
    TType Type { get; }
}

internal interface IPackageContent<TContent, TType> : IPackageContent<TType>
    where TContent : class
    where TType : struct, Enum
{
    TContent LoadContent();
}
