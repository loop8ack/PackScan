using System.CommandLine;

using PackScan.PackagesProvider.Generator;

using SixLabors.ImageSharp;

using ContentWriteModeEnum = PackScan.PackagesProvider.Generator.ContentWriteMode;
using LanguageEnum = PackScan.PackagesProvider.Generator.Language;

namespace PackScan.Tool.PackagesProvider;

internal static class GeneratePackagesProviderOptions
{
    public static void AddToCommand(Command command)
    {
        IEnumerable<Option> options = typeof(GeneratePackagesProviderOptions)
            .GetProperties()
            .Where(x => x.PropertyType.IsAssignableTo(typeof(Option)))
            .Select(x => (Option)x.GetValue(null)!);

        foreach (Option option in options)
            command.Add(option);
    }

    public static Option<string?> TargetFramework { get; } = new(new[] { "-f", "--target-framework" }, () => "", """
        Specifies the target framework for the project.
        """
    );

    public static Option<string?> RuntimeIdentifier { get; } = new(new[] { "-ri", "--runtime-identifier" }, () => "", """
        Defines the runtime identifier for the project.
        """
    );

    public static Option<string> ProjectPath { get; } = new(new[] { "-p", "--project-path" }, () => ".", """
        The path to the project file or the folder containing the project file.
        """
    );

    public static Option<string?> OutputFolder { get; } = new(new[] { "-o", "--output" }, """
        Specifies the name or path to the target directory.
        """
    )
    { IsRequired = false };

    public static Option<bool> ClearOutputFolder { get; } = new("--clear-output", () => false, """
        Determines whether the output folder should be emptied before execution (true) or not (false).
        """
    );

    // Generator
    public static Option<LanguageEnum?> Language { get; } = new(new[] { "-l", "--language" }, $"""
        Determines which programming language is to be used for generation. Currently only {LanguageEnum.CSharp} is supported.
        """
    )
    { IsRequired = false };

    public static Option<bool> Public { get; } = new("--public", """
        Determines whether the generated code should be generated public (true) or internal (false).
        """
    )
    { IsRequired = false };

    public static Option<string?> ClassName { get; } = new(new[] { "-class", "--class-name" }, """
        The name of the class through which the data should be available.
        """
    )
    { IsRequired = false };

    public static Option<string?> Namespace { get; } = new("--namespace", """
        The namespace in which the code is to be generated.
        """
    )
    { IsRequired = false };

    public static Option<bool> NullableAnnotations { get; } = new("--nullable", """
        Determines whether nullable annotations should be used (true) or not (false).
        """
    )
    { IsRequired = false };

    public static Option<bool> LoadContentsParallel { get; } = new(new[] { "-parallel", "--load-parallel" }, """
        Determines whether the packages contents should be loaded in parallel (true) or sequentially (false).
        """
    )
    { IsRequired = false };

    public static Option<ContentWriteModeEnum> ContentWriteMode { get; } = new("--content-write-mode", $"""
        Specify how the package content is to be written.
         - {ContentWriteModeEnum.Embed}: The files are treated as embedded resources.
         - {ContentWriteModeEnum.InCode}: The file contents are deposited as code in.
         - {ContentWriteModeEnum.File}: The files are copied to the output directory.

    
        """
    //The blank lines improve the readability of the output.
    )
    { IsRequired = false };

    public static Option<bool> GenerateProjectFile { get; } = new("--add-project-file", """
        Determines whether a props file should be created that registers all generated files (true) or not (false).
        """
    )
    { IsRequired = false };

    public static Option<ContentLoadMode?> ContentLoadMode { get; } = new("--load-mode", """
        Sets all load mode values.
        """
    )
    { IsRequired = false };

    public static Option<ContentLoadMode?> IconContentLoadMode { get; } = new("--icon-load-mode", $"""
        Determines if and from where the icon should be loaded.
        """
    )
    { IsRequired = false };

    public static Option<Size?> IconContentMaxSize { get; } = new("--icon-max-size", $"""
        Specifies the maximum size to which the icon should be resized.
        """
    )
    { IsRequired = false };

    public static Option<ContentLoadMode?> LicenseContentLoadMode { get; } = new("--license-load-mode", $"""
        Determines if and from where the license details should be loaded.
        """
    )
    { IsRequired = false };

    public static Option<ContentLoadMode?> ReadMeContentLoadMode { get; } = new("--readme-load-mode", $"""
        Determines if and from where the readme should be loaded.
        """
    )
    { IsRequired = false };

    public static Option<ContentLoadMode?> ReleaseNotesContentLoadMode { get; } = new("--release-notes-load-mode", $"""
        Determines if and from where the release notes should be loaded.
        """
    )
    { IsRequired = false };

    public static Option<string?> DownloadCacheFolder { get; } = new("--download-cache-folder", $"""
        Determines where downloads are to be cached.
        """
    )
    { IsRequired = false };
}
