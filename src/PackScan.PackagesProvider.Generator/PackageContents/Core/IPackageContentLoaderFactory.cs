namespace PackScan.PackagesProvider.Generator.PackageContents.Core;

internal interface IPackageContentLoaderFactory
{
    IPackageContentLoader<byte[], ImageType> CreateImageLoader();
    IPackageContentLoader<string, TextType> CreateTextLoader();
}
