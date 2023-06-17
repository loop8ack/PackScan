using PackScan.PackagesProvider;

namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// Repository metadata that allows you to associate the package with the repository that created it,
/// with the ability to get as detailed as the name of the individual branch and/or the SHA-1 hash of the commit that created the package.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#repository"/>
/// </remarks>
public interface IPackageRepositoryData
{
    /// <summary>
    /// Known repository type.
    /// </summary>
    PackageRepositoryType Type { get; }

    /// <summary>
    /// Repository type name.
    /// </summary>
    string? TypeName { get; }

    /// <summary>
    /// The URL to the repository.
    /// </summary>
    Uri? Url { get; }

    /// <summary>
    /// The branch from which the package was created.
    /// </summary>
    string? Branch { get; }

    /// <summary>
    /// The commit from which the package was created.
    /// </summary>
    string? Commit { get; }
}
