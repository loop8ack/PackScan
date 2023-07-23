using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.Build.Evaluation;

using PackScan.PackagesProvider.Generator;
using PackScan.PackagesProvider.Generator.Files;
using PackScan.PackagesProvider.Generator.Info;
using PackScan.PackagesReader;
using PackScan.PackagesReader.Abstractions;
using PackScan.Tool.Utils;

using Options = PackScan.Tool.PackagesProvider.GeneratePackagesProviderOptions;

namespace PackScan.Tool.PackagesProvider;

internal static class GeneratePackagesProviderCommand
{
    public static Command Create()
    {
        const string description = """
            This command allows you to read package information and generate code for accessing the package data.
            It provides a convenient way to retrieve comprehensive details about installed packages, including dependencies.
            Additionally, the tool generates files containing the package contents, facilitating easy access to and retrieval of this data
            """;

        Command command = new("generate-packages-provider", description);
        Options.AddToCommand(command);
        command.SetHandler(HandleCommand);

        return command;
    }

    private static void HandleCommand(InvocationContext context)
    {
        (Project project, string projectFilePath) = GetProject(context.ParseResult);
        (string outputFolderPath, bool clearOutputFolder) = GetOutputFolderConfigurations(context, project, projectFilePath);

        IReadOnlyCollection<IPackageData> packageData = CreatePackageDataReader(project, projectFilePath).Read(context.GetCancellationToken());
        IPackagesProviderFiles files = CreatePackagesProviderGeneratorOptions(context.ParseResult, project, projectFilePath).WriteCode(packageData, context.GetCancellationToken());

        if (clearOutputFolder)
            ClearFolder(outputFolderPath, context.GetCancellationToken());

        files.WriteToDirectory(outputFolderPath, context.GetCancellationToken());
    }

    private static (string outputFolderPath, bool clearOutputFolder) GetOutputFolderConfigurations(InvocationContext context, Project project, string projectFilePath)
    {
        string? outputFolderPath = context.ParseResult.GetValueForOption(Options.OutputFolder)?.Replace('\\', '/');
        bool clearOutputFolder = context.ParseResult.GetValueForOption(Options.ClearOutputFolder);

        if (outputFolderPath is null or { Length: 0 })
            outputFolderPath = project.GetPropertyValue("PackagesProviderOutputPath");

        if (outputFolderPath is null or { Length: 0 })
            outputFolderPath = ".";

        outputFolderPath = Path.Combine(Path.GetDirectoryName(projectFilePath)!, outputFolderPath);

        return (outputFolderPath, clearOutputFolder);
    }

    private static (Project project, string projectFilePath) GetProject(ParseResult parseResult)
    {
        string projectPath = parseResult.GetValueForOption(Options.ProjectPath)?.Replace('\\', '/') ?? ".";

        string projectFilePath = GetProjectFilePath(projectPath);

        Project project = new ProjectCollection()
            .LoadProject(projectFilePath);

        return (project, projectFilePath);
    }

    private static string GetProjectFilePath(string projectPath)
    {
        projectPath = Path.GetFullPath(projectPath);

        if (File.Exists(projectPath))
            return projectPath;

        string? result = null;

        if (Directory.Exists(projectPath))
        {
            foreach (string filePath in Directory.EnumerateFiles(projectPath, "*", SearchOption.TopDirectoryOnly))
            {
                string extension = Path.GetExtension(filePath).ToLower();

                bool isProjectFile = extension
                    is ".csproj"
                    or ".fsproj"
                    or ".vbproj";

                if (isProjectFile)
                {
                    if (result is not null)
                        throw new InvalidOperationException("Multiple project files found");

                    result = filePath;
                }
            }
        }

        if (result is null)
            throw new InvalidOperationException("No project file found");

        return result;
    }

    private static PackageDataReader CreatePackageDataReader(Project project, string projectFilePath)
    {
        string targetFramework = project.GetPropertyValue("TargetFramework");
        string runtimeIdentifier = project.GetPropertyValue("RuntimeIdentifier");
        string baseIntermediateOutput = project.GetPropertyValue("BaseIntermediateOutputPath").Replace('\\', '/');

        AssetsFilePath assetsFilePath = AssetsFilePath
            .FromIntermediateOutput(Path.Combine(Path.GetDirectoryName(projectFilePath)!, baseIntermediateOutput));

        return new PackageDataReader(assetsFilePath, targetFramework, runtimeIdentifier)
        {
            KnownPackageIds = project
                .GetItems("KnownPackageId")
                .Select(static x =>
                {
                    string idPrefix = x.GetMetadataValue("Identity");
                    string owner = x.GetMetadataValue("Owner");
                    string product = x.GetMetadataValue("Product");

                    return new KnownPackageId(idPrefix, owner, product);
                })
        };
    }

    private static PackagesProviderGenerator CreatePackagesProviderGeneratorOptions(ParseResult parseResult, Project project, string projectFilePath)
    {
        PackagesProviderGenerator generator = new()
        {
            ProductInfoProvider = new AssemblyProductInfoProvider(Assembly.GetExecutingAssembly())
        };

        if (parseResult.TryGetOptionValueIfSpecified(Options.Language, out Language? language) && language is not null)
            generator.Language = language.Value;
        else
        {
            string languageName = project.GetPropertyValue("Language");

            if (languageName is not null and { Length: > 0 })
            {
                generator.Language = languageName switch
                {
                    "C#" => Language.CSharp,
                    "F#" => Language.FSharp,
                    "VB" => Language.VisualBasic,
                    _ => throw new NotSupportedException($"Not supported language: {languageName}"),
                };
            }
        }

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.Public, "PackagesProviderGeneratePublic", out bool asPublic))
            generator.Public = asPublic;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ClassName, "PackagesProviderGenerateClassName", out string? className) && className is not null and { Length: > 0 })
            generator.ClassName = className;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.Namespace, "PackagesProviderGenerateNamespace", out string? @namespace))
            generator.Namespace = @namespace;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.NullableAnnotations, "PackagesProviderNullableAnnotations", out bool nullable))
            generator.NullableAnnotations = nullable;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.LoadContentsParallel, "PackagesProviderLoadContentsParallel", out bool loadContentParallel))
            generator.LoadContentParallel = loadContentParallel;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ContentWriteMode, "PackagesProviderContentWriteMode", out ContentWriteMode contentWriteMode))
            generator.ContentWriteMode = contentWriteMode;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.GenerateProjectFile, "PackagesProviderGenerateProjectFile", out bool generateProjectFile))
            generator.GenerateProjectFile = generateProjectFile;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.IconContentLoadMode, Options.ContentLoadMode, "PackagesProviderIconContentLoadMode", out ContentLoadMode? iconContentLoadMode) && iconContentLoadMode is not null)
            generator.IconContentLoadMode = iconContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.LicenseContentLoadMode, Options.ContentLoadMode, "PackagesProviderLicenseContentLoadMode", out ContentLoadMode? licenseContentLoadMode) && licenseContentLoadMode is not null)
            generator.LicenseContentLoadMode = licenseContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ReadMeContentLoadMode, Options.ContentLoadMode, "PackagesProviderReadMeContentLoadMode", out ContentLoadMode? readMeContentLoadMode) && readMeContentLoadMode is not null)
            generator.ReadMeContentLoadMode = readMeContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ReleaseNotesContentLoadMode, Options.ContentLoadMode, "PackagesProviderReleaseNotesContentLoadMode", out ContentLoadMode? releaseNotesContentLoadMode) && releaseNotesContentLoadMode is not null)
            generator.ReleaseNotesContentLoadMode = releaseNotesContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.DownloadCacheFolder, "PackagesProviderDownloadCacheFolder", out string? downloadCacheFolder) && downloadCacheFolder is not null and { Length: > 0 })
            generator.DownloadCacheFolder = Path.Combine(Path.GetDirectoryName(projectFilePath)!, downloadCacheFolder);

        return generator;

    }

    private static void ClearFolder(string folderPath, CancellationToken cancellationToken)
    {
        DirectoryInfo directory = new(folderPath);

        if (!directory.Exists)
            return;

        foreach (DirectoryInfo subDirectory in directory.GetDirectories())
        {
            cancellationToken.ThrowIfCancellationRequested();

            subDirectory.Delete(true);
        }

        foreach (FileInfo file in directory.GetFiles())
        {
            cancellationToken.ThrowIfCancellationRequested();

            file.Delete();
        }
    }
}

file static class Extensions
{
    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, string propertyName, [MaybeNull] out T value)
    {
        (ParseResult parseResult, Project project) = parseResultAndProject;

        if (parseResult.TryGetOptionValueIfSpecified(option, out value))
            return true;

        bool success = TryGetPropertyValue(project, propertyName, typeof(T), out object? obj);
        value = (T)obj!;
        return success;
    }

    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, Option<T> alternativeOption, string propertyName, [MaybeNull] out T value)
    {
        (ParseResult parseResult, Project project) = parseResultAndProject;

        if (parseResult.TryGetOptionValueIfSpecified(option, out value))
            return true;

        if (parseResult.TryGetOptionValueIfSpecified(alternativeOption, out value))
            return true;

        bool success = TryGetPropertyValue(project, propertyName, typeof(T), out object? obj);
        value = (T)obj!;
        return success;
    }

    private static bool TryGetPropertyValue(Project project, string propertyName, Type type, [MaybeNull] out object value)
    {
        string s = project.GetPropertyValue(propertyName);

        if (s is null)
        {
            value = null;
            return false;
        }

        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return Enum.TryParse(type, s, out value);

        value = Convert.ChangeType(s, type);
        return true;
    }
}
