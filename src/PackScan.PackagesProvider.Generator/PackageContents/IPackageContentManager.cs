using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesProvider.Generator.PackageContents;

internal interface IPackageContentManager : IPackageContentProvider
{
    void LoadAll(IReadOnlyCollection<IPackageData> packages, bool parallel, CancellationToken cancellationToken);
}
