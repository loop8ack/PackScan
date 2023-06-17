using System.Diagnostics.CodeAnalysis;

using NuGet.ProjectModel;

namespace PackScan.PackagesReader;

internal static class Utils
{
    public static bool TryParseHttpUrl(string? s, [MaybeNullWhen(false)] out Uri url)
    {
        if (Uri.TryCreate(s, UriKind.Absolute, out url))
        {
            bool isUrl = url.Scheme == Uri.UriSchemeHttp
                || url.Scheme == Uri.UriSchemeHttps;

            if (!isUrl)
                url = null;
        }

        return url is not null;
    }

    public static bool TryGetExistingLibraryPath(this LockFile lockFile, LockFileLibrary library, string? name, [MaybeNullWhen(false)] out string path)
    {
        path = null;

        if (name is null or { Length: 0 })
            return false;

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            if (name.Contains(c))
                return false;
        }

        foreach (LockFileItem? packageFolder in lockFile.PackageFolders)
        {
            if (packageFolder is null)
                continue;

            try
            {
                path = Path.Combine(packageFolder.Path, library.Path, name);

                if (!Path.IsPathRooted(path))
                    path = Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (File.Exists(path))
                return true;
        }

        return false;
    }
}
