using System.Diagnostics.CodeAnalysis;

namespace PackScan.PackagesProvider.Generator.Files;

internal interface IPackagesProviderFilesManager : IPackagesProviderFiles
{
    void RemovePhysicalFiles();
    bool TryGetFile(string fileName, [MaybeNullWhen(false)] out IPackagesProviderFile file);
    IPackagesProviderFile AddContents(string fileName, string contents);
    IPackagesProviderFile AddPhysical(string physicalFilePath);
    IPackagesProviderFile AddPhysical(string fileName, string physicalFilePath);
}
