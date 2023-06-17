namespace PackScan.PackagesProvider;

/// <summary>
/// Provides metadata for all packages used.
/// </summary>
public interface IPackagesProvider
{
    /// <summary>
    /// Searches for a package by ID.
    /// </summary>
    /// <param name="id">The ID to search for.</param>
    /// <returns>The found package or null if the ID is not known.</returns>
    IPackage? GetPackageById(string id);

    /// <summary>
    /// Returns all packages.
    /// </summary>
    IEnumerable<IPackage> GetPackages();
}
