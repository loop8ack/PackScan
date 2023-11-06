using System.Diagnostics.CodeAnalysis;

using NuGet.Frameworks;
using NuGet.LibraryModel;

using NuGet.Packaging;

using NuGet.ProjectModel;

using PackScan.PackagesReader.Abstractions;
using PackScan.PackagesReader.Models;

namespace PackScan.PackagesReader;

/// <summary>
/// Reads package data from a project assets file.
/// </summary>
public sealed class PackageDataReader : IPackageDataReader
{
    /// <summary>
    /// Gets the project assets file path.
    /// </summary>
    public AssetsFilePath AssetsFilePath { get; }

    /// <summary>
    /// Gets the target framework of the project.
    /// </summary>
    public string TargetFramework { get; }

    /// <summary>
    /// Gets the runtime identifier of the project (optional).
    /// </summary>
    public string? RuntimeIdentifier { get; }

    /// <summary>
    /// Represents a collection of known package IDs and their associated owners or products.
    /// </summary>
    public IEnumerable<KnownPackageId> KnownPackageIds { get; set; } = Enumerable.Empty<KnownPackageId>();

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageDataReader"/> class.
    /// </summary>
    /// <param name="assetsFilePath">The file path of the project assets file.</param>
    /// <param name="targetFramework">The target framework of the project.</param>
    /// <param name="runtimeIdentifier">The runtime identifier of the project (optional).</param>
    public PackageDataReader(string assetsFilePath, string targetFramework, string? runtimeIdentifier)
        : this(new AssetsFilePath(assetsFilePath), targetFramework, runtimeIdentifier)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageDataReader"/> class.
    /// </summary>
    /// <param name="assetsFilePath">The file path of the project assets file.</param>
    /// <param name="targetFramework">The target framework of the project.</param>
    /// <param name="runtimeIdentifier">The runtime identifier of the project (optional).</param>
    public PackageDataReader(AssetsFilePath assetsFilePath, string targetFramework, string? runtimeIdentifier)
    {
        ThrowHelper.ThrowIfNullOrEmpty(targetFramework);

        AssetsFilePath = assetsFilePath;
        TargetFramework = targetFramework;
        RuntimeIdentifier = runtimeIdentifier;
    }

    /// <summary>
    /// Reads package data from the project assets file.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token (optional).</param>
    /// <returns>An collection of <see cref="IPackageData"/>.</returns>
    public IReadOnlyCollection<IPackageData> Read(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        LockFile lockFile = AssetsFilePath.ReadLockFile();

        NuGetFramework framework = GetFramework(lockFile, TargetFramework)
            ?? throw new InvalidOperationException($"No target framework was found for the alias '{TargetFramework}'.");

        IList<LibraryDependency> projectDependencies = lockFile.PackageSpec
            .GetTargetFramework(framework)
            .Dependencies;

        LockFileTarget? target = TryGetTarget(lockFile, framework, RuntimeIdentifier)
            ?? throw new InvalidOperationException($"No target was found for the target framework '{TargetFramework}' and the runtime id '{RuntimeIdentifier}'.");

        List<IPackageData> result = new();

        foreach (LockFileTargetLibrary? targetLibrary in target.Libraries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            LockFileLibrary library = lockFile.GetLibrary(targetLibrary.Name, targetLibrary.Version);

            if (library.Type != "package")
                continue;

            if (!TryGetManifestFilePath(lockFile, library, out string? nuspecFilePath))
                continue;

            Manifest manifest;

            using (FileStream nuspecFileStream = File.OpenRead(nuspecFilePath))
                manifest = Manifest.ReadFrom(nuspecFileStream, validateSchema: false);

            result.Add(new PackageData()
            {
                LockFile = lockFile,
                Library = library,
                Manifest = manifest,
                TargetLibrary = targetLibrary,

                ProjectDependency = projectDependencies
                    .SingleOrDefault(x => x.Name == library.Name),

                KnownPackageId = KnownPackageIds
                    .OrderByDescending(x => x.IdPrefix.Length)
                    .LastOrDefault(x => manifest.Metadata.Id.StartsWith(x.IdPrefix, StringComparison.OrdinalIgnoreCase)),
            });
        };

        return result;

        static NuGetFramework? GetFramework(LockFile lockFile, string targetFramework)
        {
            return lockFile.PackageSpec.TargetFrameworks
                .FirstOrDefault(tfi => tfi.TargetAlias.Equals(targetFramework, StringComparison.OrdinalIgnoreCase))
                ?.FrameworkName;
        }

        static LockFileTarget TryGetTarget(LockFile lockFile, NuGetFramework framework, string? runtimeIdentifier)
        {
            return string.IsNullOrEmpty(runtimeIdentifier)
                ? lockFile.GetTarget(framework, null)
                : lockFile.GetTarget(framework, runtimeIdentifier)
                    ?? lockFile.GetTarget(framework, null);
        }
    }

    private static bool TryGetManifestFilePath(LockFile lockFile, LockFileLibrary library, [MaybeNullWhen(false)] out string manifestFilePath)
    {
        string manifestFileName = $"{library.Name}{PackagingConstants.ManifestExtension}";

        foreach (string? file in library.Files)
        {
            if (!Path.GetFileName(file).Equals(manifestFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            if (lockFile.TryGetExistingLibraryPath(library, file, out manifestFilePath))
                return true;
        }

        manifestFilePath = null;
        return false;
    }
}
