namespace PackScan.PackagesProvider.Generator.Files;

public interface IPackagesProviderFiles
{
    /// <summary>
    /// Gets all files.
    /// </summary>
    IReadOnlyCollection<IPackagesProviderFile> Files { get; }

    /// <summary>
    /// Writes all files to the specified directory.
    /// </summary>
    /// <param name="directoryPath">The directory path where the files will be written.</param>
    /// <param name="cancellationToken">The cancellation token (optional).</param>
    void WriteToDirectory(string directoryPath, CancellationToken cancellationToken = default);
}
