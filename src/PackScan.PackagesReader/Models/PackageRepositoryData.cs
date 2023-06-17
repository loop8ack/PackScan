using NuGet.Packaging.Core;

using PackScan.PackagesProvider;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageRepositoryData : IPackageRepositoryData
{
    private static readonly IReadOnlyDictionary<string, PackageRepositoryType> _knownRepositoryTypes
        = new Dictionary<string, PackageRepositoryType>(StringComparer.OrdinalIgnoreCase)
        {
            ["git"] = PackageRepositoryType.Git,
        };

    public PackageRepositoryType Type { get; }
    public string? TypeName { get; }
    public Uri? Url { get; }
    public string? Branch { get; }
    public string? Commit { get; }

    public PackageRepositoryData(PackageData package)
    {
        RepositoryMetadata? repository = package.Metadata.Repository;

        _knownRepositoryTypes.TryGetValue(repository?.Type ?? "", out PackageRepositoryType knownType);
        Uri.TryCreate(repository?.Url, UriKind.RelativeOrAbsolute, out Uri? repoUrl);

        Type = knownType;
        TypeName = repository?.Type;
        Branch = repository?.Branch;
        Commit = repository?.Commit;
        Url = repoUrl;
    }
}
