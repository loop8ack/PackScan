namespace PackScan.PackagesProvider.Generator.Code.Documentation;

internal interface IDocumentationReaderFactory
{
    IDocumentationReader Create(INamespaceProvider namespaceProvider);
}
