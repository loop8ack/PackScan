using PackScan.PackagesReader.Abstractions;

using Path = System.IO.Path;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core;

internal sealed class PackageContentManager : IPackageContentManager
{
    private IReadOnlyDictionary<string, PackageContents>? _contentsByPackageId;

    public required ContentLoadMode IconLoadMode { get; init; }
    public required ContentLoadMode LicenseLoadMode { get; init; }
    public required ContentLoadMode ReadMeLoadMode { get; init; }
    public required ContentLoadMode ReleaseNotesLoadMode { get; init; }

    public required IPackageContentLoader<byte[], ImageType> IconContentLoader { get; init; }
    public required IPackageContentLoader<string, TextType> LicenseContentLoader { get; init; }
    public required IPackageContentLoader<string, TextType> ReadMeContentLoader { get; init; }
    public required IPackageContentLoader<string, TextType> ReleaseNotesContentLoader { get; init; }

    public bool HasImageFiles
    {
        get
        {
            if (_contentsByPackageId is null)
                return false;

            return _contentsByPackageId.Values.Any(x => x.Icon is not null);
        }
    }
    public bool HasTextFiles
    {
        get
        {
            if (_contentsByPackageId is null)
                return false;

            return _contentsByPackageId.Values
                .Any(x => (x.License ?? x.ReadMe ?? x.ReleaseNotes) is not null);
        }
    }

    public IEnumerable<IPackageContent> AllContents
    {
        get
        {
            if (_contentsByPackageId is null)
                yield break;

            foreach (PackageContents contents in _contentsByPackageId.Values)
            {
                if (contents.Icon is not null)
                    yield return contents.Icon;

                if (contents.License is not null)
                    yield return contents.License;

                if (contents.ReadMe is not null)
                    yield return contents.ReadMe;

                if (contents.ReleaseNotes is not null)
                    yield return contents.ReleaseNotes;
            }
        }
    }

    public void LoadAll(IReadOnlyCollection<IPackageData> packages, bool parallel, CancellationToken cancellationToken)
    {
        _contentsByPackageId = parallel
            ? LoadPackagesContentsParallel(packages, cancellationToken)
            : LoadPackagesContentsSequential(packages, cancellationToken);
    }
    private IReadOnlyDictionary<string, PackageContents> LoadPackagesContentsParallel(IReadOnlyCollection<IPackageData> packages, CancellationToken cancellationToken)
    {
        Dictionary<string, PackageContents> byId = new();

        ParallelOptions options = new()
        {
            CancellationToken = cancellationToken
        };

        Parallel
            .ForEach(packages, options, package =>
            {
                PackageContents contents = LoadPackageContents(package, cancellationToken);

                lock (byId)
                    byId.Add(package.Id, contents);
            });

        return byId;
    }
    private IReadOnlyDictionary<string, PackageContents> LoadPackagesContentsSequential(IReadOnlyCollection<IPackageData> packages, CancellationToken cancellationToken)
    {
        Dictionary<string, PackageContents> byId = new();

        foreach (IPackageData package in packages)
        {
            cancellationToken.ThrowIfCancellationRequested();

            PackageContents contents = LoadPackageContents(package, cancellationToken);

            byId.Add(package.Id, contents);
        }

        return byId;
    }
    private PackageContents LoadPackageContents(IPackageData package, CancellationToken cancellationToken)
    {
        PackageContents contents = new()
        {
            Icon = IconContentLoader.TryLoad(IconLoadMode, package.Icon, cancellationToken),
            License = LicenseContentLoader.TryLoad(LicenseLoadMode, package.License, cancellationToken),
            ReadMe = ReadMeContentLoader.TryLoad(ReadMeLoadMode, package.ReadMe, cancellationToken),
            ReleaseNotes = ReleaseNotesContentLoader.TryLoad(ReleaseNotesLoadMode, package.ReleaseNotes, cancellationToken),
        };

        RenameFileToPackageId(FileExtensionMappings.ExtensionByImageType, package.Id, contents.Icon, "Icon", ".img");
        RenameFileToPackageId(FileExtensionMappings.ExtensionByTextType, package.Id, contents.License, "License", ".txt");
        RenameFileToPackageId(FileExtensionMappings.ExtensionByTextType, package.Id, contents.ReadMe, "Icon", ".txt");
        RenameFileToPackageId(FileExtensionMappings.ExtensionByTextType, package.Id, contents.ReleaseNotes, "Icon", ".txt");

        return contents;
    }
    private void RenameFileToPackageId<TContent, TType>(IReadOnlyDictionary<TType, string> extensionsByType, string packageId, IPackageContent<TContent, TType>? content, string nameSuffix, string defaultExtension)
        where TContent : class
        where TType : struct, Enum
    {
        if (content is null)
            return;

        string fileExtension;

        if (Path.HasExtension(content.File.Name))
            fileExtension = Path.GetExtension(content.File.Name);
        else if (!extensionsByType.TryGetValue(content.Type, out fileExtension))
            fileExtension = defaultExtension;

        content.File.Name = $"PackageContent.{packageId}.{nameSuffix}{fileExtension}";
    }

    public IPackageContent<byte[], ImageType>? GetPackageIcon(string packageId) => GetPackageContents(packageId)?.Icon;
    public IPackageContent<string, TextType>? GetPackageLicense(string packageId) => GetPackageContents(packageId)?.License;
    public IPackageContent<string, TextType>? GetPackageReadMe(string packageId) => GetPackageContents(packageId)?.ReadMe;
    public IPackageContent<string, TextType>? GetPackageReleaseNotes(string packageId) => GetPackageContents(packageId)?.ReleaseNotes;
    private PackageContents? GetPackageContents(string packageId)
    {
        if (_contentsByPackageId is null)
            return null;

        if (_contentsByPackageId.TryGetValue(packageId, out PackageContents? contentFiles))
            return contentFiles;

        return null;
    }

    private sealed class PackageContents
    {
        public IPackageContent<byte[], ImageType>? Icon;
        public IPackageContent<string, TextType>? License;
        public IPackageContent<string, TextType>? ReadMe;
        public IPackageContent<string, TextType>? ReleaseNotes;
    }
}
