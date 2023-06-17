using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using PackScan.Analyzer.Core.Options;
using PackScan.PackagesReader;
using PackScan.PackagesReader.Abstractions;

using static PackScan.PackagesReader.AssetsFilePath;

namespace PackScan.Analyzer.Core.Services;

internal sealed class PackageDataReaderService
{
    public OptionValue<string> TargetFramework { get; }
    public OptionValue<string?> RuntimeIdentifier { get; }
    public OptionValue<string> ProjectDirectory { get; }
    public OptionValue<string> BaseIntermediateOutputPath { get; }
    public OptionValue<string?> AssetsFilePath { get; }
    public OptionValue<string?> KnownPackageIds { get; }

    public PackageDataReaderService(AnalyzerConfigOptions options)
    {
        TargetFramework = options.GetOptionString("TargetFramework");
        RuntimeIdentifier = options.GetOptionNullableString("RuntimeIdentifier");
        ProjectDirectory = options.GetOptionString("MSBuildProjectDirectory");
        BaseIntermediateOutputPath = options.GetOptionString("BaseIntermediateOutputPath");
        KnownPackageIds = options.GetOptionNullableString("_KnownPackageIds");
    }

    public void ValidateOptions(ICollection<Diagnostic> diagnostics)
    {
        TargetFramework.Validate(diagnostics);
        RuntimeIdentifier.Validate(diagnostics);
        ProjectDirectory.Validate(diagnostics);
        BaseIntermediateOutputPath.Validate(diagnostics);
        AssetsFilePath.Validate(diagnostics);
        KnownPackageIds.Validate(diagnostics);
    }

    public IReadOnlyCollection<IPackageData> Read()
    {
        AssetsFilePath assetsFilePath = AssetsFilePath.Value is null or { Length: 0 }
            ? FromIntermediateOutput(ProjectDirectory, BaseIntermediateOutputPath)
            : new AssetsFilePath(AssetsFilePath.Value);

        IEnumerable<KnownPackageId> knownPackageIds = ParseKnownPackageIds(KnownPackageIds.Value).ToArray();

        IPackageDataReader reader = new PackageDataReader(assetsFilePath, TargetFramework, RuntimeIdentifier, knownPackageIds);

        return reader.Read();
    }

    private static IEnumerable<KnownPackageId> ParseKnownPackageIds(string? s)
    {
        if (s is null or { Length: 0 })
            yield break;

        foreach (string entry in s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string[] idAndData = entry.Split('=');

            if (idAndData.Length < 2)
                continue;

            string id = idAndData[0];
            string data = string.Join("=", idAndData.Skip(1));
            string[] values = data.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            string? owner = values.Length > 0 ? values[0] : null;
            string? product = values.Length > 1 ? values[1] : null;

            yield return new KnownPackageId(id, owner, product);
        }
    }

}
