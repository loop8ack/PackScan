using Microsoft.CodeAnalysis;

using PackScan.Analyzer.Core;
using PackScan.Analyzer.Core.Services;
using PackScan.PackagesProvider.Generator;

namespace PackScan.Analyzer;

public abstract class PackagesProviderGenerator : IIncrementalGenerator
{
    static PackagesProviderGenerator()
        => EmbeddedAssemblyLoader.Init();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterSourceOutput(context.AnalyzerConfigOptionsProvider
            .Select((p, c) => new PackagesProviderGeneratorService(p.GlobalOptions)),
            (context, service) => service.Generate(context));
    }
}
