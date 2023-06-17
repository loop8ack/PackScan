using PackScan.PackagesProvider.Generator.Info;
using PackScan.PackagesProvider.Generator.PackageContents;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesProvider.Generator.Code;

internal class PackagesProviderWriterOptions
{
    public required bool Public { get; init; }
    public required string? Namespace { get; init; }
    public required bool NullableAnnotations { get; init; }
    public required ContentWriteMode ContentWriteMode { get; init; }
    public required bool GenerateProjectFile { get; init; }
    public required IProductInfoProvider? ProductInfoProvider { get; init; }
    public required CancellationToken CancellationToken { get; init; }
    public required string ClassName { get; init; }
    public required IReadOnlyList<IPackageData> Packages { get; init; }
    public required IPackageContentProvider ContentProvider { get; init; }
    public required IReadOnlyDictionary<string, string> PropertyNamesByPackageId { get; init; }
}
