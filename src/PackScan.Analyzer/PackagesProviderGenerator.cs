using Microsoft.CodeAnalysis;

using PackScan.Analyzer.Core;
using PackScan.Analyzer.Core.Services;

namespace PackScan.Analyzer;

[Generator(LanguageNames.CSharp)]
public sealed class PackagesProviderGenerator : IIncrementalGenerator
{
    static PackagesProviderGenerator()
        => EmbeddedAssemblyLoader.Init();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterImplementationSourceOutput(context.AnalyzerConfigOptionsProvider
            .Select((p, c) => new PackagesProviderGeneratorService(p.GlobalOptions)),
            (context, service) => service.Generate(context));
    }
}
