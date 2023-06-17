using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace PackScan;

internal static class ThrowHelper
{
    public static void ThrowIfNull([NotNull] object? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null)
            throw new ArgumentNullException(paramName);
    }

    public static void ThrowIfNullOrEmpty([NotNull] string? argument, [CallerArgumentExpression(nameof(argument))] string? paramName = null)
    {
        if (argument is null or { Length: 0 })
            throw new ArgumentNullException(paramName);
    }
}
