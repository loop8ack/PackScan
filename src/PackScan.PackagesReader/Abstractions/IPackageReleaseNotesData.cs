namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// Release notes metadata of the package. It can provide the contents of the release notes.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#releasenotes"/>
/// </remarks>
public interface IPackageReleaseNotesData : IPackageContentData
{
    /// <summary>
    /// The original value of the release notes entry.
    /// </summary>
    string? Value { get; }
}
