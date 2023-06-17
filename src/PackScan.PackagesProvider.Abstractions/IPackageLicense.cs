namespace PackScan.PackagesProvider;

/// <summary>
/// License metadata of the package. It can provide more data or the content of the license.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#license"/>
/// </remarks>
public interface IPackageLicense
{
    /// <summary>
    /// The URL of the license, if specified.
    /// </summary>
    Uri? Url { get; }

    /// <summary>
    /// An SPDX license expression or null if only a license file has been specified.
    /// </summary>
    string? Expression { get; }

    /// <summary>
    /// The version of the license, if specified.
    /// </summary>
    Version? Version { get; }

    /// <summary>
    /// The content of the license.
    /// <br/>
    /// Determined by nuget.org, only <see cref="TextType.Plain"/> or <see cref="TextType.Markdown"/> is supported.
    /// </summary>
    /// <remarks>
    /// This may be null if the package does not provide a license or if loading the readme has been disabled.
    /// </remarks>
    string? Text { get; }

    /// <summary>
    /// The format in which the license content was specified.
    /// <br/>
    /// Determined by nuget.org, only <see cref="TextType.Plain"/> or <see cref="TextType.Markdown"/> is supported.
    /// </summary>
    TextType Type { get; }
}
