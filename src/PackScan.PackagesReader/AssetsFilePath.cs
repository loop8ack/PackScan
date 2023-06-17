using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

using NuGet.ProjectModel;

namespace PackScan.PackagesReader;

/// <summary>
/// Represents a file path for project assets.
/// </summary>
public readonly struct AssetsFilePath
{
    /// <summary>
    /// Gets the file name of the project assets file.
    /// </summary>
    public static readonly string FileName = LockFileFormat.AssetsFileName;

    private const string DefaultBaseIntermediateOutputPath = "obj";

    /// <summary>
    /// Creates an <see cref="AssetsFilePath"/> instance based on the current C# project.
    /// <br/>
    /// This method is intended for testing purposes so that the file path of the executed project can be used.
    /// </summary>
    /// <param name="baseIntermediateOutputPath">The base intermediate output path (default is "obj").</param>
    /// <param name="currentFilePath">The path of the current file (automatically populated).</param>
    /// <returns>An <see cref="AssetsFilePath"/> instance representing the project assets file path for the current C# project.</returns>
    public static AssetsFilePath FromCurrentCSharpProject(string baseIntermediateOutputPath = DefaultBaseIntermediateOutputPath, [CallerFilePath] string currentFilePath = null!)
        => FromCurrentProject(".csproj", baseIntermediateOutputPath, currentFilePath);

    /// <summary>
    /// Creates an <see cref="AssetsFilePath"/> instance based on the current project.
    /// <br/>
    /// This method is intended for testing purposes so that the file path of the executed project can be used.
    /// </summary>
    /// <param name="projectFileExtension">The extension of the project file (e.g., ".csproj").</param>
    /// <param name="baseIntermediateOutputPath">The base intermediate output path (default is "obj").</param>
    /// <param name="currentFilePath">The path of the current file (automatically populated).</param>
    /// <returns>An <see cref="AssetsFilePath"/> instance representing the project assets file path for the current project.</returns>
    public static AssetsFilePath FromCurrentProject(string projectFileExtension, string baseIntermediateOutputPath = DefaultBaseIntermediateOutputPath, [CallerFilePath] string currentFilePath = null!)
    {
        ThrowHelper.ThrowIfNullOrEmpty(projectFileExtension);
        ThrowHelper.ThrowIfNullOrEmpty(baseIntermediateOutputPath);
        ThrowHelper.ThrowIfNullOrEmpty(currentFilePath);

        string filter = $"*{projectFileExtension}";
        string projectPath = Path.GetDirectoryName(currentFilePath)!;
        string[] projectFiles = Directory.GetFiles(projectPath, filter, SearchOption.TopDirectoryOnly);

        while (projectFiles.Length == 0)
        {
            projectPath = Path.GetDirectoryName(currentFilePath)!;

            if (projectPath is null)
                break;

            projectFiles = Directory.GetFiles(projectPath, filter, SearchOption.TopDirectoryOnly);
        }

        if (projectFiles.Length == 0)
            throw new InvalidOperationException("Cannot find matching project file");

        if (projectFiles.Length > 1)
            throw new InvalidOperationException("Several project files were found.");

        return new AssetsFilePath(Path.Combine(projectPath!, baseIntermediateOutputPath, FileName));
    }

    /// <summary>
    /// Creates an <see cref="AssetsFilePath"/> instance based on the intermediate output of a project.
    /// </summary>
    /// <param name="projectPath">The path of the project.</param>
    /// <param name="baseIntermediateOutputPath">The base intermediate output path (default is "obj").</param>
    /// <returns>An <see cref="AssetsFilePath"/> instance representing the project assets file path for the specified project.</returns>
    public static AssetsFilePath FromIntermediateOutput(string projectPath, string baseIntermediateOutputPath = DefaultBaseIntermediateOutputPath)
    {
        ThrowHelper.ThrowIfNullOrEmpty(projectPath);
        ThrowHelper.ThrowIfNullOrEmpty(baseIntermediateOutputPath);

        if (File.Exists(projectPath))
            projectPath = Path.GetDirectoryName(projectPath)!;

        return new AssetsFilePath(Path.Combine(projectPath, baseIntermediateOutputPath, FileName));
    }

    /// <summary>
    /// Creates an <see cref="AssetsFilePath"/> instance based on the intermediate output path.
    /// </summary>
    /// <param name="baseIntermediateOutputPath">The base intermediate output path.</param>
    /// <returns>An <see cref="AssetsFilePath"/> instance representing the project assets file path in the intermediate output directory.</returns>
    public static AssetsFilePath FromIntermediateOutput(string baseIntermediateOutputPath)
    {
        ThrowHelper.ThrowIfNullOrEmpty(baseIntermediateOutputPath);

        return new AssetsFilePath(Path.Combine(baseIntermediateOutputPath, FileName));
    }

    /// <summary>
    /// Gets the file path associated with the <see cref="AssetsFilePath"/>.
    /// </summary>
    [MaybeNull]
    public string FilePath { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AssetsFilePath"/> struct.
    /// </summary>
    /// <param name="filePath">The file path.</param>
    public AssetsFilePath(string filePath)
    {
        ThrowHelper.ThrowIfNullOrEmpty(filePath);

        FilePath = filePath;
    }

    /// <summary>
    /// Reads the lock file from the project assets file.
    /// </summary>
    /// <returns>The <see cref="LockFile"/> instance representing the lock file.</returns>
    internal LockFile ReadLockFile()
    {
        if (string.IsNullOrEmpty(FilePath))
            throw new InvalidOperationException("Assets file path is empty");

        if (!File.Exists(FilePath))
            throw new FileNotFoundException($"Assets file path does not exist: {Path.GetFullPath(FilePath)}");

        return new LockFileFormat().Read(FilePath);
    }
}
