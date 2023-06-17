namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// Readme metadata of the package. It can provide the content of the readme.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#readme"/>
/// </remarks>
public interface IPackageReadMeData : IPackageContentData
{
    /// <summary>
    /// The original value of the readme entry.
    /// </summary>
    string? Value { get; }
}
