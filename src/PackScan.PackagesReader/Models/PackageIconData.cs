using NuGet.Packaging;

using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageIconData : IPackageIconData
{
    public Uri? Url { get; }
    public string? FilePath { get; }

    public PackageIconData(PackageData package)
    {
        ManifestMetadata metadata = package.Metadata;

        Utils.TryParseHttpUrl(metadata.Icon, out Uri? url);

        if (!package.LockFile.TryGetExistingLibraryPath(package.Library, metadata.Icon, out string? filePath))
            package.LockFile.TryGetExistingLibraryPath(package.Library, "Icon.png", out filePath);

        Url = metadata.IconUrl ?? url;
        FilePath = filePath;
    }
}
