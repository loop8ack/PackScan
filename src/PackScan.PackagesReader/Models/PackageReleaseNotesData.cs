using PackScan.PackagesReader;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageReleaseNotesData : IPackageReleaseNotesData
{
    public Uri? Url { get; }
    public string? FilePath { get; }
    public string? Value { get; }

    public PackageReleaseNotesData(PackageData package)
    {
        string? releaseNotes = package.Metadata.ReleaseNotes;

        Utils.TryParseHttpUrl(releaseNotes, out Uri? url);
        package.LockFile.TryGetExistingLibraryPath(package.Library, releaseNotes, out string? filePath);

        Url = url;
        FilePath = filePath;
        Value = releaseNotes;
    }
}
