namespace PackScan.PackagesProvider;

/// <summary>
/// Represents the image format.
/// <para>
/// Used in:<br/>
/// <see cref="IPackageIcon.Type"/><br/>
/// </para>
/// </summary>
public enum ImageType
{
    /// <summary>
    /// Unknown image format
    /// </summary>
    Unknown,

    /// <summary>
    /// GIF format
    /// </summary>
    Gif,

    /// <summary>
    /// PNG format
    /// </summary>
    Png,

    /// <summary>
    /// JPEG format
    /// </summary>
    Jpeg,

    /// <summary>
    /// TIFF format
    /// </summary>
    Tiff,
}
