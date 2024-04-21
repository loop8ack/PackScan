using PackScan.PackagesProvider.Generator.Files;

using Stream = System.IO.Stream;
using StreamReader = System.IO.StreamReader;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal sealed class PackageTextContentLoader : PackageContentLoader<string, TextType>
{
    protected override IReadOnlyDictionary<string, TextType> MimeTypeTypeMapping => FileExtensionMappings.TextTypeByMimeType;
    protected override IReadOnlyDictionary<string, TextType> FileExtensionTypeMapping => FileExtensionMappings.TextTypeByExtension;

    public PackageTextContentLoader(IPackagesProviderFilesManager filesManager, IHttpClientFactory httpClientFactory, IPackagesProviderFileModification? modification, PackageContentLoaderOptions options)
        : base(filesManager, httpClientFactory, modification, options)
    {
    }

    protected override TextType GetContentType(HttpResponseMessage response, string filePath)
    {
        TextType type = base.GetContentType(response, filePath);

        if (ContainsHtml(type, filePath))
            type = TextType.Html;

        return type;
    }
    protected override TextType GetContentType(string filePath)
    {
        TextType type = base.GetContentType(filePath);

        if (ContainsHtml(type, filePath))
            type = TextType.Html;

        return type;
    }

    private bool ContainsHtml(TextType type, string filePath)
    {
        if (type != TextType.Plain)
            return false;

        using Stream fileStream = File.OpenRead(filePath);
        using StreamReader fileReader = new(fileStream);

        // Read a maximum of three lines to search for "html"
        for (int i = 0; i < 3; i++)
        {
            string? line = fileReader.ReadLine();

            if (line?.Contains("<html") == true)
                return true;
        }

        return false;
    }

    protected override PackageContent<string, TextType> CreateContent(IPackagesProviderFile file, TextType type)
        => new PackageTextContent(file, type);
}
