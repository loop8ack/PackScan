using Microsoft.CodeAnalysis.Diagnostics;

using PackScan.PackagesProvider.Generator;

namespace PackScan.Analyzer.Core.Options;

internal static class KnownOptions
{
    private static readonly IReadOnlyDictionary<string, Language> _languageValueMapping =
        new Dictionary<string, Language>(StringComparer.OrdinalIgnoreCase)
        {
            ["C"] = Language.CSharp,
            ["F"] = Language.FSharp,
            ["VB"] = Language.VisualBasic,
        };

    public static OptionValue<Language> GetLanguage(this AnalyzerConfigOptions options, string name) => options.GetOptionValue(name, "Language", _languageValueMapping);
}
