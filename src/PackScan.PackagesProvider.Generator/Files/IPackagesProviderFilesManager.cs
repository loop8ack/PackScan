using System.Diagnostics.CodeAnalysis;

namespace PackScan.PackagesProvider.Generator.Files;

internal interface IPackagesProviderFilesManager : IPackagesProviderFiles
{
    void RemovePhysicalFiles();
    bool TryGetFile(string fileName, [MaybeNullWhen(false)] out IPackagesProviderFile file);
    IPackagesProviderFile AddContents(string fileName, string contents, IPackagesProviderFileModification? modification = null);
    IPackagesProviderFile AddPhysical(string physicalFilePath, IPackagesProviderFileModification? modification = null);
}
