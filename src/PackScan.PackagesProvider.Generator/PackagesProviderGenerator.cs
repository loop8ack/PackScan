using System.Text;

using PackScan.PackagesProvider.Generator.Code;
using PackScan.PackagesProvider.Generator.Code.CSharp;
using PackScan.PackagesProvider.Generator.Code.Documentation;
using PackScan.PackagesProvider.Generator.Files;
using PackScan.PackagesProvider.Generator.Files.Core;
using PackScan.PackagesProvider.Generator.Files.Modifications;
using PackScan.PackagesProvider.Generator.Info;
using PackScan.PackagesProvider.Generator.PackageContents;
using PackScan.PackagesProvider.Generator.PackageContents.Core;
using PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;
using PackScan.PackagesProvider.Generator.Utils;
using PackScan.PackagesReader.Abstractions;

using SixLabors.ImageSharp;

namespace PackScan.PackagesProvider.Generator;

public sealed class PackagesProviderGenerator
{
    private readonly StringBuilder _tmpStringBuilder = new();
    private string? _className;

    public Language Language { get; set; } = Language.CSharp;
    public bool Public { get; set; } = false;
    public string ClassName
    {
        get => _className is null or { Length: 0 } ? "Packages" : _className;
        set => _className = value;
    }
    public string? Namespace { get; set; }
    public bool NullableAnnotations { get; set; } = true;
    public bool LoadContentParallel { get; set; } = false;
    public bool GenerateProjectFile { get; set; } = true;
    public IProductInfoProvider? ProductInfoProvider { get; set; }
    public ContentWriteMode ContentWriteMode { get; set; } = ContentWriteMode.Embed;
    public ContentLoadMode IconContentLoadMode { get; set; }
    public Size? IconContentMaxSize { get; set; }
    public ContentLoadMode LicenseContentLoadMode { get; set; }
    public ContentLoadMode ReadMeContentLoadMode { get; set; }
    public ContentLoadMode ReleaseNotesContentLoadMode { get; set; }
    public string? DownloadCacheFolder { get; set; }
    public TimeSpan? DownloadCacheAccessTimeout { get; set; }
    public TimeSpan? DownloadCacheAccessRetryDelay { get; set; }

    public IPackagesProviderFiles WriteCode(IEnumerable<IPackageData> packages, CancellationToken cancellationToken = default)
    {
        ThrowHelper.ThrowIfNull(packages);

        using HttpClientFactory httpClientFactory = new();

        IPackageData[] packagesArray = packages.OrderBy(x => x.Id).ToArray();

        PackagesProviderFilesManager filesManager = new();

        IPackageContentManager contentManager = CreateAndLoadContentManager(httpClientFactory, filesManager, packagesArray, cancellationToken);
        PackagesProviderWriterBase writer = CreateWriter(packagesArray, contentManager, cancellationToken);

        writer.CreateCode(filesManager);

        if (ContentWriteMode == ContentWriteMode.InCode)
            filesManager.RemovePhysicalFiles();

        return filesManager;
    }

    private IPackageContentManager CreateAndLoadContentManager(HttpClientFactory httpClientFactory, IPackagesProviderFilesManager filesManager, IPackageData[] packagesData, CancellationToken cancellationToken)
    {
        PackageContentLoaderOptions contentLoaderOptions = new()
        {
            DownloadCacheFolder = DownloadCacheFolder is null or { Length: 0 }
                ? Path.Combine(Path.GetTempPath(), "PackScan.PackagesProvider.Writer", "DownloadCache")
                : Environment.ExpandEnvironmentVariables(DownloadCacheFolder),

            DownloadCacheAccessTimeout = DownloadCacheAccessTimeout ?? TimeSpan.FromSeconds(1),
            DownloadCacheAccessRetryDelay = DownloadCacheAccessRetryDelay ?? TimeSpan.FromMilliseconds(10)!,
        };

        IPackagesProviderFileModification? iconContentModification = IconContentMaxSize is not null
            ? new ReduceImageContentSizeModification(IconContentMaxSize.Value)
            : null;

        PackageContentManager contentManager = new()
        {
            IconLoadMode = IconContentLoadMode,
            IconContentLoader = new PackageImageContentLoader(filesManager, httpClientFactory, iconContentModification, contentLoaderOptions),

            LicenseLoadMode = LicenseContentLoadMode,
            LicenseContentLoader = new PackageTextContentLoader(filesManager, httpClientFactory, modification: null, contentLoaderOptions),

            ReadMeLoadMode = ReadMeContentLoadMode,
            ReleaseNotesContentLoader = new PackageTextContentLoader(filesManager, httpClientFactory, modification: null, contentLoaderOptions),

            ReleaseNotesLoadMode = ReleaseNotesContentLoadMode,
            ReadMeContentLoader = new PackageTextContentLoader(filesManager, httpClientFactory, modification: null, contentLoaderOptions),
        };

        contentManager.LoadAll(packagesData, LoadContentParallel, cancellationToken);

        return contentManager;
    }

    private PackagesProviderWriterBase CreateWriter(IPackageData[] packagesData, IPackageContentManager contentManager, CancellationToken cancellationToken)
    {
        PackagesProviderWriterOptions options = new()
        {
            Public = Public,
            ClassName = ClassName,
            Namespace = Namespace,
            NullableAnnotations = NullableAnnotations,
            ContentWriteMode = ContentWriteMode,
            GenerateProjectFile = GenerateProjectFile,
            ProductInfoProvider = ProductInfoProvider,
            CancellationToken = cancellationToken,

            Packages = packagesData,
            ContentProvider = contentManager,
            PropertyNamesByPackageId = GetPropertyNamesByPackageId(packagesData, cancellationToken),
        };

        DocumentationReaderFactory docReaderFactory = new();

        return Language switch
        {
            Language.CSharp => new CSharpPackagesProviderWriter(docReaderFactory, options),
            _ => throw new NotSupportedException($"The language '{Language}' is not supported")
        };
    }
    private IReadOnlyDictionary<string, string> GetPropertyNamesByPackageId(IEnumerable<IPackageData> packages, CancellationToken cancellationToken)
    {
        Dictionary<string, string> result = new();

        foreach (IPackageData package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string propertyName = GetPropertyNameFromId(package.Id);

            result.Add(package.Id, propertyName);
        }

        return result;
    }
    private string GetPropertyNameFromId(string packageId)
    {
        for (int i = 0; i < packageId.Length; i++)
        {
            char c = packageId[i];

            bool isDigit = c is >= '0' and <= '9';
            bool isLetter = false
                || c is >= 'A' and <= 'Z'
                || c is >= 'a' and <= 'z'
                || c is 'A' or 'Ö' or 'Ü'
                || c is 'ä' or 'ö' or 'ü'
                || c is '_';

            if (isDigit && i == 0)
                _tmpStringBuilder.Append('_');

            if (isDigit || isLetter)
                _tmpStringBuilder.Append(c);
            else
                _tmpStringBuilder.Append('_');
        }

        string result = _tmpStringBuilder.ToString();
        _tmpStringBuilder.Clear();
        return result;
    }
}
