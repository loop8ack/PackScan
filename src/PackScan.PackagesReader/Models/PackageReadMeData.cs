using PackScan.PackagesReader;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageReadMeData : IPackageReadMeData
{
    public Uri? Url { get; }
    public string? FilePath { get; }
    public string? Value { get; }

    public PackageReadMeData(PackageData package)
    {
        string? readme = package.Metadata.Readme;

        Utils.TryParseHttpUrl(readme, out Uri? url);
        package.LockFile.TryGetExistingLibraryPath(package.Library, readme, out string? filePath);

        Value = readme;
        FilePath = filePath;
        Url = url;
    }
}
