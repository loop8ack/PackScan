using System.Net.Mime;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core;

internal static class FileExtensionMappings
{
    public static readonly IReadOnlyDictionary<string, ImageType> ImageTypeByExtension
        = new Dictionary<string, ImageType>(StringComparer.OrdinalIgnoreCase)
        {
            [".png"] = ImageType.Png,
            [".gif"] = ImageType.Gif,
            [".jpg"] = ImageType.Jpeg,
            [".jpeg"] = ImageType.Jpeg,
            [".tiff"] = ImageType.Tiff,
        };

    public static readonly IReadOnlyDictionary<string, ImageType> ImageTypeByMimeType
        = new Dictionary<string, ImageType>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/png"] = ImageType.Png,
            [MediaTypeNames.Image.Gif] = ImageType.Gif,
            [MediaTypeNames.Image.Jpeg] = ImageType.Jpeg,
            [MediaTypeNames.Image.Tiff] = ImageType.Tiff,
        };

    public static readonly IReadOnlyDictionary<TextType, string> ExtensionByTextType
        = new Dictionary<TextType, string>()
        {
            [TextType.Html] = ".html",
            [TextType.RichText] = ".rtf",
            [TextType.Plain] = ".txt",
            [TextType.Markdown] = ".md",
        };

    public static readonly IReadOnlyDictionary<string, TextType> TextTypeByExtension
        = new Dictionary<string, TextType>(StringComparer.OrdinalIgnoreCase)
        {
            [".htm"] = TextType.Html,
            [".html"] = TextType.Html,
            [".rtf"] = TextType.RichText,
            [".txt"] = TextType.Plain,
            [".md"] = TextType.Markdown,
        };

    public static readonly IReadOnlyDictionary<string, TextType> TextTypeByMimeType
        = new Dictionary<string, TextType>(StringComparer.OrdinalIgnoreCase)
        {
            [MediaTypeNames.Text.Html] = TextType.Html,
            [MediaTypeNames.Text.RichText] = TextType.RichText,
            [MediaTypeNames.Text.Plain] = TextType.Plain,
        };

    public static readonly IReadOnlyDictionary<ImageType, string> ExtensionByImageType
        = new Dictionary<ImageType, string>()
        {
            [ImageType.Png] = ".png",
            [ImageType.Gif] = ".gif",
            [ImageType.Jpeg] = ".jpg",
            [ImageType.Tiff] = ".tiff",
        };

}
