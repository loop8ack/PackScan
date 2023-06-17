using System.CommandLine;
using System.CommandLine.Parsing;

namespace PackScan.Tool.Utils;

internal static class ParseResultExtensions
{
    public static bool TryGetOptionValueIfSpecified<T>(this ParseResult parseResult, Option<T> option, out T? value)
    {
        if (!IsSpecified(parseResult, option))
        {
            value = default;
            return false;
        }

        value = parseResult.GetValueForOption(option);
        return true;
    }

    public static bool IsSpecified(this ParseResult parseResult, Option option)
    {
        foreach (Token token in parseResult.Tokens)
        {
            if (option.Aliases.Contains(token.Value))
                return true;
        }

        return false;
    }
}
