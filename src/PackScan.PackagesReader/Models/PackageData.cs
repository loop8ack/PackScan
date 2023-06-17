using System.Diagnostics;

using NuGet.LibraryModel;
using NuGet.Packaging;
using NuGet.ProjectModel;

using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesReader.Models;

[DebuggerDisplay("{Id}")]
internal sealed class PackageData : IPackageData
{
    private IPackageVersionData? _version;
    private IPackageRepositoryData? _repository;
    private IPackageReleaseNotesData? _releaseNotes;
    private IPackageReadMeData? _readMe;
    private IPackageLicenseData? _license;
    private IPackageIconData? _icon;

    public required LockFile LockFile { get; init; }
    public required LockFileLibrary Library { get; init; }
    public required LockFileTargetLibrary TargetLibrary { get; init; }
    public required Manifest Manifest { get; init; }
    public required LibraryDependency? ProjectDependency { get; init; }
    public required KnownPackageId? KnownPackageId { get; init; }

    public ManifestMetadata Metadata => Manifest.Metadata;

    public string Id => Metadata.Id;
    public string? Description => Metadata.Description;
    public string? Owner => KnownPackageId?.Owner;
    public string? Product => KnownPackageId?.Product;
    public string? Title => Metadata.Title;
    public IReadOnlyList<string> Authors => Metadata.Authors?.ToArray() ?? Array.Empty<string>();
    public Uri? ProjectUrl => Metadata.ProjectUrl;
    public string? Copyright => Metadata.Copyright;
    public IReadOnlyList<string> Tags => Metadata.Tags?.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
    public bool IsProjectDependency => ProjectDependency is not null;
    public bool IsDevelopmentDependency => Metadata.DevelopmentDependency;
    public string? Language => Metadata.Language;
    public IEnumerable<string> DependencyPackageIds => TargetLibrary.Dependencies.Select(x => x.Id);

    IPackageVersionData IPackageData.Version => _version ??= new PackageVersionData(this);
    IPackageRepositoryData IPackageData.Repository => _repository ??= new PackageRepositoryData(this);
    IPackageReleaseNotesData IPackageData.ReleaseNotes => _releaseNotes ??= new PackageReleaseNotesData(this);
    IPackageReadMeData IPackageData.ReadMe => _readMe ??= new PackageReadMeData(this);
    IPackageLicenseData IPackageData.License => _license ??= new PackageLicenseData(this);
    IPackageIconData IPackageData.Icon => _icon ??= new PackageIconData(this);
}
