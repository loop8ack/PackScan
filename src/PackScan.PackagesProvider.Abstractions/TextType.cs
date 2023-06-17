namespace PackScan.PackagesProvider;

/// <summary>
/// Text format used in various metadata properties.
/// <para>
/// Used in:<br/>
/// <see cref="IPackageReadMe.Type"/><br/>
/// <see cref="IPackageLicense.Type"/><br/>
/// <see cref="IPackageReleaseNotes.Type"/>
/// </para>
/// </summary>
public enum TextType
{
    /// <summary>
    /// Plain text, no format known
    /// </summary>
    Plain,

    /// <summary>
    /// HTML format
    /// </summary>
    Html,

    /// <summary>
    /// RTF format
    /// </summary>
    RichText,

    /// <summary>
    /// Markdown or MD format
    /// </summary>
    Markdown
}
