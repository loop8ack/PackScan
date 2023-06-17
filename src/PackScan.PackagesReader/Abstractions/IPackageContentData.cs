namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// Base interface for package content data.
/// </summary>
public interface IPackageContentData
{
    /// <summary>
    /// Gets the URL associated with the package content, if available.
    /// </summary>
    Uri? Url { get; }

    /// <summary>
    /// Gets the file path of the package content, if available.
    /// </summary>
    string? FilePath { get; }
}
