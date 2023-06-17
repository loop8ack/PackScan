namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// License metadata of the package. It can provide the content of the license.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#license"/>
/// </remarks>
public interface IPackageLicenseData : IPackageContentData
{
    /// <summary>
    /// An SPDX license expression or null if only a license file has been specified.
    /// </summary>
    string? Expression { get; }

    /// <summary>
    /// The version of the license, if specified.
    /// </summary>
    Version? Version { get; }
}
