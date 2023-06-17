namespace PackScan.PackagesReader.Abstractions;

/// <summary>
/// Icon metadata of the package. It can provide the content of the icon.
/// </summary>
/// <remarks>
/// Documentation: <see href="https://learn.microsoft.com/en-us/nuget/reference/nuspec#icon"/>
/// </remarks>
public interface IPackageIconData : IPackageContentData
{
}
