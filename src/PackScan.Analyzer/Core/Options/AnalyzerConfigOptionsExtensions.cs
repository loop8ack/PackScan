using Microsoft.CodeAnalysis.Diagnostics;

using SixLabors.ImageSharp;

namespace PackScan.Analyzer.Core.Options;

internal static class AnalyzerConfigOptionsExtensions
{
    private const string Prefix = "build_property.";

    public static OptionValue<string> GetOptionString(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? value))
            return new(name, value);

        return new(name, Diagnostics.OptionNotFound.Create(name));
    }

    public static OptionValue<string?> GetOptionNullableString(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? value))
            return new(name, value);

        return new(name, (string?)null);
    }

    public static OptionValue<bool> GetOptionBool(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            if (bool.TryParse(str, out bool value))
                return new(name, value);

            return new(name, Diagnostics.OptionNotParsedBool.Create(name, str));
        }

        return new(name, Diagnostics.OptionNotFound.Create(name));
    }

    public static OptionValue<TEnum> GetOptionEnum<TEnum>(this AnalyzerConfigOptions options, string name)
        where TEnum : struct, Enum
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            if (Enum.TryParse(str, ignoreCase: true, out TEnum value))
                return new(name, value);

            return new(name, Diagnostics.OptionNotParsedEnum.Create<TEnum>(name, str));
        }

        return new(name, Diagnostics.OptionNotFound.Create(name));
    }

    public static OptionValue<TimeSpan> GetOptionTimeSpan(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            if (TimeSpan.TryParse(str, out TimeSpan value))
                return new(name, value);

            return new(name, Diagnostics.OptionNotParsedTimeSpan.Create(name, str));
        }

        return new(name, Diagnostics.OptionNotFound.Create(name));
    }

    public static OptionValue<Size?> GetOptionNullableSKSize(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            if (string.IsNullOrEmpty(str))
                return new(name, (Size?)null);

            return Utils.TryParseImageSharpSize(str, out Size? value)
                ? new(name, value)
                : new(name, Diagnostics.OptionNotParsedSize.Create(name, str));
        }

        return new(name, (Size?)null);
    }

    public static OptionValue<TValue> GetOptionValue<TValue>(this AnalyzerConfigOptions options, string name, string typeName, IReadOnlyDictionary<string, TValue> valueMapping)
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            if (valueMapping.TryGetValue(str, out TValue? value))
                return new(name, value);

            return new(name, Diagnostics.OptionNotParsedValue.Create(name, str, typeName, valueMapping.Keys));
        }

        return new(name, Diagnostics.OptionNotFound.Create(name));
    }

    public static OptionValue<IReadOnlyList<string>> GetOptionStringList(this AnalyzerConfigOptions options, string name)
    {
        if (options.TryGetValue(Prefix + name, out string? str))
        {
            string[] values = str
                ?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                ?? Array.Empty<string>();

            return new(name, values);
        }

        return new(name, Array.Empty<string>());
    }
}
