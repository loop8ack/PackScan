using NuGet.Packaging;

using PackScan.PackagesReader;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageLicenseData : IPackageLicenseData
{
    public string? Expression { get; }
    public Version? Version { get; }
    public Uri? Url { get; }
    public string? FilePath { get; }

    public PackageLicenseData(PackageData package)
    {
        LicenseMetadata? licenseMetadata = package.Metadata.LicenseMetadata;

        Uri? licenseUrl = package.Metadata.LicenseUrl;
        string? licenseFile = null;
        string? licenseExpression = null;

        if (licenseMetadata is not null)
        {
            licenseUrl ??= licenseMetadata.LicenseUrl;

            switch (licenseMetadata.Type)
            {
                case LicenseType.File:
                    if (!package.LockFile.TryGetExistingLibraryPath(package.Library, licenseMetadata.License, out licenseFile))
                        package.LockFile.TryGetExistingLibraryPath(package.Library, "license.txt", out licenseFile);
                    break;

                case LicenseType.Expression:
                    package.LockFile.TryGetExistingLibraryPath(package.Library, "license.txt", out licenseFile);
                    licenseExpression = licenseMetadata.License;
                    break;

                default:
                    throw new InvalidOperationException($"Unknown license value type: {licenseMetadata.Type}");
            }
        }

        Expression = licenseExpression;
        Version = licenseMetadata?.Version;
        FilePath = licenseFile;
        Url = licenseUrl;
    }
}
