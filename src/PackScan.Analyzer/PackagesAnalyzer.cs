using System.Collections.Immutable;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using PackScan.Analyzer.Core;
using PackScan.Analyzer.Core.Services;

namespace PackScan.Analyzer;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PackagesAnalyzer : DiagnosticAnalyzer
{
    static PackagesAnalyzer()
        => EmbeddedAssemblyLoader.Init();

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.AllDescriptors;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationAction(AnalyzeLicenses);
    }

    private void AnalyzeLicenses(CompilationAnalysisContext context)
    {
        new PackageAllowedLicensesAnalyzerService(context.Options.AnalyzerConfigOptionsProvider.GlobalOptions)
            .AnalyzeLicenses(context);
    }
}
