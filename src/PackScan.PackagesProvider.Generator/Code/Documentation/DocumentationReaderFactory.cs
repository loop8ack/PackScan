namespace PackScan.PackagesProvider.Generator.Code.Documentation;

internal class DocumentationReaderFactory : IDocumentationReaderFactory
{
    public IDocumentationReader Create(INamespaceProvider namespaceProvider)
        => new DocumentationReader(namespaceProvider);
}
