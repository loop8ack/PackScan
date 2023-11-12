using System.Diagnostics.CodeAnalysis;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace PackScan.PackagesProvider.Generator.Files.Modifications;

internal class ReduceImageContentSizeModification : IPackagesProviderFileModification
{
    private readonly Size _maxSize;

    public ReduceImageContentSizeModification(Size maxSize)
    {
        _maxSize = maxSize;
    }

    public bool TryModifyText(string text, [MaybeNullWhen(false)] out string result)
    {
        result = null;
        return false;
    }

    public bool TryModifyStream(Stream source, Stream output)
    {
        try
        {
            using (Image image = Image.Load(source, out IImageFormat format))
            {
                double ratioX = (double)_maxSize.Width / image.Width;
                double ratioY = (double)_maxSize.Height / image.Height;
                double ratio = Math.Min(ratioX, ratioY);

                if (ratio >= 1)
                    return false;

                int newWidth = (int)(image.Width * ratio);
                int newHeight = (int)(image.Height * ratio);

                image.Mutate(x => x.Resize(newWidth, newHeight));
                image.Save(output, format);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }
}
