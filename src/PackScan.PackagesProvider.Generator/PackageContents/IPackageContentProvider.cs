namespace PackScan.PackagesProvider.Generator.PackageContents;

internal interface IPackageContentProvider
{
    IEnumerable<IPackageContent> AllContents { get; }

    bool HasImageFiles { get; }
    bool HasTextFiles { get; }

    IPackageContent<byte[], ImageType>? GetPackageIcon(string packageId);
    IPackageContent<string, TextType>? GetPackageLicense(string packageId);
    IPackageContent<string, TextType>? GetPackageReadMe(string packageId);
    IPackageContent<string, TextType>? GetPackageReleaseNotes(string packageId);
}
