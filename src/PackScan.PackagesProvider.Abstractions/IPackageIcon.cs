namespace PackScan.PackagesProvider;

/// <summary>
/// Icon metadata of the package. It can provide more data or the content of the icon.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon"/>
/// </remarks>
public interface IPackageIcon
{
    /// <summary>
    /// The url to the icon, if specified.
    /// </summary>
    Uri? Url { get; }

    /// <summary>
    /// Indicates if the content of the package icon is available.
    /// </summary>
    bool HasContent { get; }

    /// <summary>
    /// The format in which the icon was specified.
    /// </summary>
    ImageType Type { get; }

    /// <summary>
    /// Opens a reading <see cref="Stream"/>
    /// </summary>
    /// <returns>A read-only <see cref="Stream"/></returns>
    Stream OpenStream();
}
