using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core;

internal interface IPackageContentLoader<TContent, TType>
    where TContent : class
    where TType : struct, Enum
{
    IPackageContent<TContent, TType>? TryLoad(ContentLoadMode loadMode, IPackageContentData? contentData, CancellationToken cancellationToken);
}
