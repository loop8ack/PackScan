namespace PackScan.PackagesProvider;

/// <summary>
/// The version of the package, following the major.minor.patch pattern. Version numbers may include a pre-release suffix as described in Package versioning.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#version"/>
/// </remarks>
public interface IPackageVersion
{
    /// <summary>
    /// The original value of the package version entry.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// Indicates whether the package version is a pre-release version or not.
    /// </summary>
    bool IsPrerelease { get; }

    /// <summary>
    /// The pre-release label of the package version.
    /// </summary>
    string Release { get; }

    /// <summary>
    /// The list of pre-release labels of the package version.
    /// </summary>
    IReadOnlyList<string> ReleaseLabels { get; }

    /// <summary>
    /// Indicates whether the package version has metadata or not.
    /// </summary>
    bool HasMetadata { get; }

    /// <summary>
    /// The specified metadata of the package version.
    /// </summary>
    string Metadata { get; }

    /// <summary>
    /// The .NET representation of the package version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Indicates whether the package version is a legacy version or not.
    /// </summary>
    bool IsLegacyVersion { get; }

    /// <summary>
    /// Specifies whether certain semantics of SemVer v2.0.0 are supported by the package version.
    /// </summary>
    bool IsSemVer2 { get; }
}
