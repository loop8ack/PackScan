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

using SixLabors.ImageSharp;

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

        IReadOnlyCollection<IPackageData> packageData = CreatePackageDataReader(context.ParseResult, project, projectFilePath).Read(context.GetCancellationToken());
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

    private static PackageDataReader CreatePackageDataReader(ParseResult parseResult, Project project, string projectFilePath)
    {
        (parseResult, project).TryGetOptionOrPropertyValue(Options.TargetFramework, "TargetFramework", out string? targetFramework);
        (parseResult, project).TryGetOptionOrPropertyValue(Options.RuntimeIdentifier, "RuntimeIdentifier", out string? runtimeIdentifier);

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

        if (parseResult.TryGetOptionValueIfSpecified(Options.Language, tryParse: null, out Language? language) && language is not null)
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

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.IconContentMaxSize, "PackagesProviderIconContentMaxSize", out Size? iconContentMaxSize))
            generator.IconContentMaxSize = iconContentMaxSize;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.LicenseContentLoadMode, Options.ContentLoadMode, "PackagesProviderLicenseContentLoadMode", out ContentLoadMode? licenseContentLoadMode) && licenseContentLoadMode is not null)
            generator.LicenseContentLoadMode = licenseContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ReadMeContentLoadMode, Options.ContentLoadMode, "PackagesProviderReadMeContentLoadMode", out ContentLoadMode? readMeContentLoadMode) && readMeContentLoadMode is not null)
            generator.ReadMeContentLoadMode = readMeContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.ReleaseNotesContentLoadMode, Options.ContentLoadMode, "PackagesProviderReleaseNotesContentLoadMode", out ContentLoadMode? releaseNotesContentLoadMode) && releaseNotesContentLoadMode is not null)
            generator.ReleaseNotesContentLoadMode = releaseNotesContentLoadMode.Value;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.DownloadCacheFolder, "PackagesProviderDownloadCacheFolder", out string? downloadCacheFolder) && downloadCacheFolder is not null and { Length: > 0 })
            generator.DownloadCacheFolder = Path.Combine(Path.GetDirectoryName(projectFilePath)!, downloadCacheFolder);

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.DownloadCacheAccessTimeout, "PackagesProviderDownloadCacheAccessTimeout", TimeSpan.TryParse, out TimeSpan downloadCacheAccessTimeout) )
            generator.DownloadCacheAccessTimeout = downloadCacheAccessTimeout;

        if ((parseResult, project).TryGetOptionOrPropertyValue(Options.DownloadCacheAccessRetryDelay, "PackagesProviderDownloadCacheAccessRetryDelay", TimeSpan.TryParse, out TimeSpan downloadCacheAccessRetryDelay))
            generator.DownloadCacheAccessRetryDelay = downloadCacheAccessRetryDelay;

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
    public static bool TryGetOptionOrPropertyValue(this (ParseResult parseResult, Project project) parseResultAndProject, Option<Size?> option, string propertyName, [MaybeNull] out Size? value)
        => TryGetOptionOrPropertyValue(parseResultAndProject, option, propertyName, PackScan.Utils.TryParseImageSharpSize, out value);

    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, string propertyName, [MaybeNull] out T value)
        => TryGetOptionOrPropertyValue(parseResultAndProject, option, propertyName, tryParse: null, out value);
    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, string propertyName, TryParse<T>? tryParse, [MaybeNull] out T value)
    {
        (ParseResult parseResult, Project project) = parseResultAndProject;

        if (parseResult.TryGetOptionValueIfSpecified(option, tryParse, out value))
            return true;

        return TryGetPropertyValue<T>(project, propertyName, tryParse, out value);
    }

    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, Option<T> alternativeOption, string propertyName, [MaybeNull] out T value)
        => TryGetOptionOrPropertyValue(parseResultAndProject, option, alternativeOption, propertyName, tryParse: null, out value);
    public static bool TryGetOptionOrPropertyValue<T>(this (ParseResult parseResult, Project project) parseResultAndProject, Option<T> option, Option<T> alternativeOption, string propertyName, TryParse<T>? tryParse, [MaybeNull] out T value)
    {
        (ParseResult parseResult, Project project) = parseResultAndProject;

        if (parseResult.TryGetOptionValueIfSpecified(option, tryParse, out value))
            return true;

        if (parseResult.TryGetOptionValueIfSpecified(alternativeOption, tryParse, out value))
            return true;

        return TryGetPropertyValue(project, propertyName, tryParse, out value);
    }

    private static bool TryGetPropertyValue<T>(Project project, string propertyName, TryParse<T>? tryParse, [MaybeNull] out T value)
    {
        string s = project.GetPropertyValue(propertyName);

        if (s is null)
        {
            value = default;
            return false;
        }

        if (tryParse?.Invoke(s, out value) == true)
            return true;

        if (TryConvert(s, typeof(T), out object? obj))
        {
            value = (T)obj!;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryConvert(string s, Type type, [MaybeNull] out object value)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;

        if (type.IsEnum)
            return Enum.TryParse(type, s, out value);

        try
        {
            value = Convert.ChangeType(s, type);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}
