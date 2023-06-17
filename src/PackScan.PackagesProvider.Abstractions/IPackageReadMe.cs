namespace PackScan.PackagesProvider;

/// <summary>
/// Readme metadata of the package. It can provide more data or the content of the readme.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#readme"/>
/// </remarks>
public interface IPackageReadMe
{
    /// <summary>
    /// The original value of the readme entry.
    /// </summary>
    string Value { get; }

    /// <summary>
    /// The content of the readme file.
    /// <br/>
    /// Determined by nuget.org, only <see cref="TextType.Markdown"/> is supported.
    /// </summary>
    /// <remarks>
    /// This may be null if the package does not provide a readme or if loading the readme has been disabled.
    /// </remarks>
    string? Text { get; }

    /// <summary>
    /// The format in which the readme notes were specified.
    /// <br/>
    /// Determined by nuget.org, only <see cref="TextType.Markdown"/> is supported.
    /// </summary>
    TextType Type { get; }
}
