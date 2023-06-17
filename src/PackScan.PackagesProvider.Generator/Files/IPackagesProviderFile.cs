using Stream = System.IO.Stream;

namespace PackScan.PackagesProvider.Generator.Files;

public interface IPackagesProviderFile
{
    /// <summary>
    /// Gets the relative file name of this file.
    /// </summary>
    string Name { get; set; }

    string ReadAllText();
    byte[] ReadAllBytes();

    /// <summary>
    /// Writes the file to the specified stream.
    /// </summary>
    /// <param name="output">The stream to write the file content to.</param>
    void CopyTo(Stream output);
}
