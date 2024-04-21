using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

using PackScan.Analyzer.Core.Options;
using PackScan.PackagesProvider.Generator;
using PackScan.PackagesProvider.Generator.Files;
using PackScan.PackagesProvider.Generator.Info;
using PackScan.PackagesReader.Abstractions;

using SixLabors.ImageSharp;

using CodeGenerator = PackScan.PackagesProvider.Generator.PackagesProviderGenerator;

namespace PackScan.Analyzer.Core.Services;

internal record class PackagesProviderGeneratorService
{
    private readonly PackageDataReaderService _readerService;

    private OptionValue<Language> Language { get; set; }
    private OptionValue<bool> IsEnabled { get; set; }
    private OptionValue<bool> Public { get; set; }
    private OptionValue<string> ClassName { get; set; }
    private OptionValue<string?> Namespace { get; set; }
    private OptionValue<bool> NullableAnnotations { get; set; }
    private OptionValue<bool> LoadContentParallel { get; set; }
    private OptionValue<ContentLoadMode> IconContentLoadMode { get; set; }
    private OptionValue<Size?> IconContentMaxSize { get; set; }
    private OptionValue<ContentLoadMode> LicenseContentLoadMode { get; set; }
    private OptionValue<ContentLoadMode> ReadMeContentLoadMode { get; set; }
    private OptionValue<ContentLoadMode> ReleaseNotesContentLoadMode { get; set; }
    private OptionValue<string> DownloadCacheFolder { get; set; }
    private OptionValue<TimeSpan> DownloadCacheAccessTimeout { get; set; }
    private OptionValue<TimeSpan> DownloadCacheAccessRetryDelay { get; set; }
    
    public PackagesProviderGeneratorService(AnalyzerConfigOptions options)
    {
        _readerService = new(options);

        Language = options.GetLanguage("Language");
        IsEnabled = options.GetOptionBool("PackagesProviderGenerationEnabled");
        Public = options.GetOptionBool("PackagesProviderGeneratePublic");
        ClassName = options.GetOptionString("PackagesProviderGenerateClassName");
        Namespace = options.GetOptionNullableString("PackagesProviderGenerateNamespace");
        NullableAnnotations = options.GetOptionBool("PackagesProviderNullableAnnotations");
        LoadContentParallel = options.GetOptionBool("PackagesProviderLoadContentsParallel");
        IconContentLoadMode = options.GetOptionEnum<ContentLoadMode>("PackagesProviderIconContentLoadMode");
        IconContentMaxSize = options.GetOptionNullableSKSize("PackagesProviderIconContentMaxSize");
        LicenseContentLoadMode = options.GetOptionEnum<ContentLoadMode>("PackagesProviderLicenseContentLoadMode");
        ReadMeContentLoadMode = options.GetOptionEnum<ContentLoadMode>("PackagesProviderReadMeContentLoadMode");
        ReleaseNotesContentLoadMode = options.GetOptionEnum<ContentLoadMode>("PackagesProviderReleaseNotesContentLoadMode");
        DownloadCacheFolder = options.GetOptionString("PackagesProviderDownloadCacheFolder");
        DownloadCacheAccessTimeout = options.GetOptionTimeSpan("PackagesProviderDownloadCacheAccessTimeout");
        DownloadCacheAccessRetryDelay = options.GetOptionTimeSpan("PackagesProviderDownloadCacheAccessRetryDelay");
    }

    public void Generate(SourceProductionContext context)
    {
        if (!IsEnabled)
            return;

        IReadOnlyList<Diagnostic> diagnostics = ValidateOptions();

        if (diagnostics.Count > 0)
        {
            foreach (Diagnostic diagnostic in diagnostics)
                context.ReportDiagnostic(diagnostic);

            return;
        }

        CoreGenerate(context);
    }

    private IReadOnlyList<Diagnostic> ValidateOptions()
    {
        List<Diagnostic> diagnostics = new();

        _readerService.ValidateOptions(diagnostics);

        Language.Validate(diagnostics);
        Public.Validate(diagnostics);
        ClassName.Validate(diagnostics);
        Namespace.Validate(diagnostics);
        NullableAnnotations.Validate(diagnostics);
        LoadContentParallel.Validate(diagnostics);
        IconContentLoadMode.Validate(diagnostics);
        IconContentMaxSize.Validate(diagnostics);
        LicenseContentLoadMode.Validate(diagnostics);
        ReadMeContentLoadMode.Validate(diagnostics);
        ReleaseNotesContentLoadMode.Validate(diagnostics);

        return diagnostics;
    }

    private void CoreGenerate(SourceProductionContext context)
    {
        IReadOnlyCollection<IPackageData> packagesData = _readerService.Read();

        IPackagesProviderFiles files = new CodeGenerator()
        {
            Language = Language,
            Public = Public,
            ClassName = ClassName,
            Namespace = Namespace,
            NullableAnnotations = NullableAnnotations,
            LoadContentParallel = LoadContentParallel,
            GenerateProjectFile = false,
            ContentWriteMode = ContentWriteMode.InCode,
            IconContentLoadMode = IconContentLoadMode,
            IconContentMaxSize = IconContentMaxSize,
            LicenseContentLoadMode = LicenseContentLoadMode,
            ReadMeContentLoadMode = ReadMeContentLoadMode,
            ReleaseNotesContentLoadMode = ReleaseNotesContentLoadMode,
            ProductInfoProvider = new AssemblyProductInfoProvider(Assembly.GetExecutingAssembly()),
            DownloadCacheFolder = DownloadCacheFolder,
            DownloadCacheAccessTimeout = DownloadCacheAccessTimeout,
            DownloadCacheAccessRetryDelay = DownloadCacheAccessRetryDelay,
        }.WriteCode(packagesData, context.CancellationToken);

        foreach (IPackagesProviderFile file in files.Files)
        {
            using MemoryStream memory = new();

            file.CopyTo(memory);

            memory.Position = 0;

            context.AddSource(file.Name, SourceText.From(memory, Encoding.UTF8, canBeEmbedded: true));
        }
    }
}
