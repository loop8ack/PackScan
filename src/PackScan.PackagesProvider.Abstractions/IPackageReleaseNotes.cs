namespace PackScan.PackagesProvider;

/// <summary>
/// Release notes metadata of the package. It may provide additional data or the contents of the release notes.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#releasenotes"/>
/// </remarks>
public interface IPackageReleaseNotes
{
    /// <summary>
    /// The URL to the release notes, if specified.
    /// </summary>
    Uri? Url { get; }

    /// <summary>
    /// The original value of the release notes entry.
    /// </summary>
    string? Value { get; }

    /// <summary>
    /// A description of the changes made in this version of the package.
    /// </summary>
    /// <remarks>
    /// This can be null if the package does not provide release notes or loading of release notes has been disabled.
    /// </remarks>
    string? Text { get; }

    /// <summary>
    /// The format in which the release notes were specified.
    /// </summary>
    TextType Type { get; }
}
