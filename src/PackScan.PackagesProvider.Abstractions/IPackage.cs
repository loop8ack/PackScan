namespace PackScan.PackagesProvider;

/// <summary>
/// Metadata of a package
/// </summary>
public interface IPackage
{
    /// <summary>
    /// The case-insensitive package identifier that is unique on nuget.org or in the gallery where the package is located.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#id"/>
    /// </remarks>
    string Id { get; }

    /// <summary>
    /// The version of the package, following the major.minor.patch pattern. Version numbers may include a pre-release suffix as described in Package versioning.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#version"/>
    /// </remarks>
    IPackageVersion Version { get; }

    /// <summary>
    /// The owner of the NuGet package.
    /// </summary>
    string? Owner { get; }

    /// <summary>
    /// The specific software product or framework that the NuGet package is associated with.
    /// </summary>
    string? Product { get; }

    /// <summary>
    /// A description of the package for UI display.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#description"/>
    /// </remarks>
    string? Description { get; }

    /// <summary>
    /// A human-friendly title of the package which may be used in some UI displays.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#title"/>
    /// </remarks>
    string? Title { get; }

    /// <summary>
    /// A list of packages authors, matching the profile names on nuget.org. These are displayed in the NuGet Gallery on nuget.org and are used to cross-reference packages by the same authors.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#authors"/>
    /// </remarks>
    IReadOnlyList<string> Authors { get; }

    /// <summary>
    /// A URL for the package's home page, often shown in UI displays as well as nuget.org.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#projecturl"/>
    /// </remarks>
    Uri? ProjectUrl { get; }

    /// <summary>
    /// Copyright details for the package.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#copyright"/>
    /// </remarks>
    string? Copyright { get; }

    /// <summary>
    /// A list of tags and keywords that describe the package and aid discoverability of packages through search and filtering.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#tags"/>
    /// </remarks>
    IReadOnlyList<string> Tags { get; }

    /// <summary>
    /// Specifies whether the package is defined as a direct dependency for the project, or is used indirectly (by another package or project).
    /// </summary>
    bool IsProjectDependency { get; }

    /// <summary>
    /// Specifies whether the package is be marked as a development-only-dependency, which prevents the package from being included as a dependency in other packages.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#developmentdependency"/>
    /// </remarks>
    bool IsDevelopmentDependency { get; }

    /// <summary>
    /// Release notes metadata of the package. It may provide additional data or the contents of the release notes.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#releasenotes"/>
    /// </remarks>
    IPackageReleaseNotes? ReleaseNotes { get; }

    /// <summary>
    /// The locale ID for the package.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#language"/>
    /// </remarks>
    string? Language { get; }

    /// <summary>
    /// Repository metadata that allows you to associate the package with the repository that created it,
    /// with the ability to get as detailed as the name of the individual branch and/or the SHA-1 hash of the commit that created the package.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#repository"/>
    /// </remarks>
    IPackageRepository? Repository { get; }

    /// <summary>
    /// Readme metadata of the package. It can provide more data or the content of the readme.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#readme"/>
    /// </remarks>
    IPackageReadMe? Readme { get; }

    /// <summary>
    /// License metadata of the package. It can provide more data or the content of the license.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#license"/>
    /// </remarks>
    IPackageLicense? License { get; }

    /// <summary>
    /// Icon metadata of the package. It can provide more data or the content of the icon.
    /// </summary>
    /// <remarks>
    /// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon"/>
    /// </remarks>
    IPackageIcon? Icon { get; }

    /// <summary>
    /// A collection of zero or more ids specifying the dependencies for the package.
    /// <br/>
    /// Use <see cref="DependencyPackages"/> to get the dependencies as <see cref="IPackage"/>.
    /// </summary>
    IEnumerable<string> DependencyPackageIds { get; }

    /// <summary>
    /// A collection of zero or more <see cref="IPackage"/> objects specifying the dependencies for the package.
    /// <br/>
    /// Use <see cref="DependencyPackageIds"/> to get only the ids of the dependencies.
    /// </summary>
    IEnumerable<IPackage> DependencyPackages { get; }
}
