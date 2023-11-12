using System.Diagnostics.CodeAnalysis;

namespace PackScan.PackagesProvider.Generator.Files;

internal interface IPackagesProviderFileModification
{
    bool TryModifyText(string text, [MaybeNullWhen(false)] out string result);
    bool TryModifyStream(Stream source, Stream output);
}
