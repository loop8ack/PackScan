using NuGet.Versioning;

using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

internal sealed class PackageVersionData : IPackageVersionData
{
    public string Value { get; }
    public bool IsPrerelease { get; }
    public string Release { get; }
    public IReadOnlyList<string> ReleaseLabels { get; }
    public bool HasMetadata { get; }
    public string Metadata { get; }
    public Version Version { get; }
    public bool IsLegacyVersion { get; }
    public bool IsSemVer2 { get; }

    public PackageVersionData(PackageData package)
    {
        NuGetVersion version = package.Metadata.Version;

        Value = version.OriginalVersion;
        IsPrerelease = version.IsPrerelease;
        Release = version.Release;
        ReleaseLabels = version.ReleaseLabels.ToArray();
        HasMetadata = version.HasMetadata;
        Metadata = version.Metadata;
        Version = version.Version;
        IsLegacyVersion = version.IsLegacyVersion;
        IsSemVer2 = version.IsSemVer2;
    }
}
