namespace PackScan.PackagesProvider.Generator;

/// <summary>
/// Specifies the content write mode options.
/// </summary>
public enum ContentWriteMode
{
    /// <summary>
    /// Embed the content files directly within the project.
    /// </summary>
    Embed,

    /// <summary>
    /// Include the content as code within the project.
    /// </summary>
    InCode,

    /// <summary>
    /// Copy the content files to the project's output directory.
    /// </summary>
    File
}
