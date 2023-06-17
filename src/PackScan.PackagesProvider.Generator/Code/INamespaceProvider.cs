using System.Diagnostics.CodeAnalysis;

namespace PackScan.PackagesProvider.Generator.Code;

internal interface INamespaceProvider
{
    bool TryGetForWrite(string? ns, [MaybeNullWhen(false)] out string result);
}
