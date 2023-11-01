namespace PackScan.PackagesProvider.Generator;

/// <summary>
/// Specifies the content load mode options.
/// </summary>
public enum ContentLoadMode
{
    /// <summary>
    /// No content should be loaded.
    /// </summary>
    None,

    /// <summary>
    /// Prefer loading content from a URL if available. If not available, fallback to loading from a file.
    /// </summary>
    PreferUrl,

    /// <summary>
    /// Prefer loading content from a file if available. If not available, fallback to loading from a URL.
    /// </summary>
    PreferFile,

    /// <summary>
    /// Load content only from a URL.
    /// </summary>
    OnlyUrl,

    /// <summary>
    /// Load content only from a file.
    /// </summary>
    OnlyFile,

    /// <summary>
    /// Load the content only from a URL if no file was provided in the package.
    /// </summary>
    UrlIfHasNoFile,

    /// <summary>
    /// Load the content only from a file if no URL was provided in the package.
    /// </summary>
    FileIfHasNoUrl,
}
