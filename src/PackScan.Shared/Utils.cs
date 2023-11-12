#if PackScan_Analyzer || PackScan_Tool
using System.Diagnostics.CodeAnalysis;

using SixLabors.ImageSharp;

namespace PackScan;

internal static class Utils
{
    [SuppressMessage("Style", "IDE0057:Use range operator")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression")]
    public static bool TryParseImageSharpSize(string s, out Size? size)
    {
        size = null;
        s = s.Trim();

        if (s.Length == 0)
            return false;

        int xIndex = s.IndexOf("x", StringComparison.OrdinalIgnoreCase);

        if (xIndex <= 0)
            return false;

        string sWidth = s.Substring(0, xIndex).Trim();
        string sHeight = s.Substring(xIndex + 1).Trim();

        bool success = false
            | int.TryParse(sWidth, out int width)
            | int.TryParse(sHeight, out int height);

        if (!success)
            return false;

        size = new(width, height);
        return true;
    }
}
#endif
