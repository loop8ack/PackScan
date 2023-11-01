[![Nuget](https://img.shields.io/nuget/v/PackScan.Tool?label=PackScan.Tool)](https://www.nuget.org/packages/PackScan.Tool)

[![Nuget](https://img.shields.io/nuget/v/PackScan.Analyzer?label=PackScan.Analyzer)](https://www.nuget.org/packages/PackScan.Analyzer)

[![Nuget](https://img.shields.io/nuget/v/PackScan.PackagesProvider.Abstractions?label=PackScan.PackagesProvider.Abstractions)](https://www.nuget.org/packages/PackScan.PackagesProvider.Abstractions)

[![Nuget](https://img.shields.io/nuget/v/PackScan.Defaults?label=PackScan.Defaults)](https://www.nuget.org/packages/PackScan.Defaults)

1. [Overview](#package-data-retrieval--license-analyzer)
   - [PackScan.Tool](#packscantool)
   - [PackScan.Analyzer](#packscananalyzer)
2. [Installation](#installation)
   - [Analyzer](#analyzer)
   - [.NET Tool](#net-tool)
3. [Generated Result](#generated-result)
4. [Configuration](#configuration)
   - [Samples](#samples)
   - [Analyzer](#analyzer-1)
   - [.NET Tool](#net-tool-1)
   - [Enums](#enums)
5. [Future?](#future)
6. [Contributing](#contributing)
7. [ChangeLog](#changelog)
8. [License](#license)

# Package Data Retrieval & License Analyzer

This project provides tools for retrieving information about installed packages and their dependencies within your code.
It allows you to retrieve comprehensive information about installed packages, including dependencies, and generates code to access this data.
The included analyzer helps you ensure license compliance by identifying packages with disallowed licenses.

Additionally, these tools offer optional configurations to generate code that provides access to icons, license texts, ReadMe content, release notes, and other package information.
You can include these additional resources based on your specific needs.

Currently, the project only supports C#, but it is designed in a way that allows for future support of VB.NET or F#.


### Key Features

1. **Generate Package Data:**</br>
Generate simple and easily accessible code to retrieve detailed information about the packages installed in your project.
With this code, you can easily access package versions, dependencies, licenses, and other data.

3. **Check License Compliance:**</br>
The included analyzer helps you ensure compliance with licensing requirements.
Identify packages that contain licenses not compatible with your project or company policies, enabling you to address potential licensing issues early on.

5. **Customizable Configuration:**</br>
Tailor the behavior of the tool to perfectly match your requirements.
Adjust the configuration to change how package contents are loaded, code is generated, and license analyses are performed.

7. **Seamless Project Integration:**</br>
The project can be seamlessly integrated into your existing projects.
Install the required NuGet packages and configure the settings accordingly. Accessing package data and license information is straightforward through the generated code.

9. **Possible Use with CI/CD:**</br>
The .NET Tool allows integration into CI/CD processes, automating package analysis and management in your continuous integration and deployment workflows.

### **[PackScan.Tool](https://www.nuget.org/packages/PackScan.Tool)**

The .NET Tool provides integration into CI/CD processes and supports different ways of depositing content files.

### **[PackScan.Analyzer](https://www.nuget.org/packages/PackScan.Analyzer)**

The Analyzer can be configured directly in your project and supports checking which packages and licenses are allowed.

# Installation

### Analyzer

Install the analyzer:

```
dotnet add package --prerelease PackScan.Analyzer
```

Additionally, you need to install the following package in all projects where you want to use the code:

```
dotnet add package --prerelease PackScan.PackagesProvider.Abstractions
```

### .NET Tool

Install the .NET Tool:

```
dotnet tool install --prerelease --global PackScan.Tool
```

It is also necessary to install the following package to enable all configuration features:

```
dotnet add package --prerelease PackScan.Defaults
```

Additionally, you need to install the following package in all projects where you want to use the code:

```
dotnet add package --prerelease PackScan.PackagesProvider.Abstractions
```

# Generated Result

The generated code allows you to retrieve package information wherever needed in your code.
You can also configure the license analyzer to enforce license compliance.

Here's how you can use the generated code:

```csharp
var serilog = Packages.GetPackageById("Serilog");

foreach (var package in Packages.GetPackages())
{
    Console.WriteLine($"{package.Id} | {package.Version.Value} | {package.License?.Expression}");
}

// or with an instance

IPackagesProvider instance = new Packages();

var serilog = instance.GetPackageById("Serilog");

foreach (var package in instance.GetPackages())
{
    Console.WriteLine($"{package.Id} | {package.Version.Value} | {package.License?.Expression}");
}

// or with dependency injection

var services = new ServiceCollection()
    .AddSingleton<IPackagesProvider, Packages>()
    .BuildServiceProvider();

var packagesProvider = services.GetRequiredService<IPackagesProvider>();
var serilog = instance.GetPackageById("Serilog");

foreach (var package in instance.GetPackages())
{
    Console.WriteLine($"{package.Id} | {package.Version.Value} | {package.License?.Expression}");
}

```

# Configuration

Both the .NET Tool and the Analyzer offer similar configurations, but there are differences due to technical reasons. The following descriptions outline all the settings and specify whether and how they can be utilized for each respective option.

## Samples

### Project Configuration

```xml
<PropertyGroup>
    <AllowedLicensesAnalyzationEnabled>true</AllowedLicensesAnalyzationEnabled>
    <AnalyzePackageDependencyLicenses>true</AnalyzePackageDependencyLicenses>
    <PackagesProviderGenerateClassName>MyPackages</PackagesProviderGenerateClassName>
    <PackagesProviderLicenseContentLoadMode>PreferFile</PackagesProviderLicenseContentLoadMode>
</PropertyGroup>

<ItemGroup>
    <KnownPackageId Include="Serilog" Owner="Serilog" Product="Serilog" />
</ItemGroup>

<ItemGroup>
    <AllowedLicense Include="Apache-2.0" />
    <AllowedLicenseByPackage Include="XUnit" />
    <AllowedLicenseByOwner Include="Microsoft" />
</ItemGroup>
```

### .NET Tool

```
rem Navigate to the project directory
cd ./src/SampleProject/

rem Generate
dotnet package-tools generate-packages-provider --output PackagesProvider --load-mode onlyfile --content-write-mode Embed --clear-output True
```

## Analyzer

### Project Properties

- **AllowedLicensesAnalyzationEnabled**</br>
Determines whether to check the licenses of the installed packages (`true`) or not (`false`).</br>
This option should be preferred over disabling all warnings since it does not require package data to be loaded.</br>
Default value: `true`

- **AnalyzePackageDependencyLicenses**</br>
Determines whether the packages not directly referenced by the project (dependencies of other packages) should also be checked (`true`) or not (`false`).</br>
Default value: `true`

- **AllowEmptyLicenses**</br>
Determines whether empty licenses should be allowed (`true`) or not (`false`).</br>
Default value: `false`

- **PackagesProviderGenerationEnabled**</br>
Indicates whether the code should be generated.</br>
Default value: `true`

- **PackagesProviderGeneratePublic**</br>
Determines whether the code should be generated as public (`true`) or internal (`false`).</br>
Default value: `false`

- **PackagesProviderGenerateClassName**</br>
The name of the class through which the data should be available.</br>
Default value: `Packages`

- **PackagesProviderGenerateNamespace**</br>
The namespace in which the code should be generated.</br>
Default value: The root namespace of the project

- **PackagesProviderNullableAnnotations**</br>
Determines whether nullable annotations should be used (`true`) or not (`false`).</br>
Default value: The Nullable setting of the project

- **PackagesProviderLoadContentsParallel**</br>
Determines whether the package contents should be loaded in parallel (`true`) or sequentially (`false`).</br>
Default value: `false`

- **PackagesProviderContentLoadMode**</br>
Overwrites the default values of the following load-mode values. [Here](#content-load-mode), you will find the supported values.</br>
Default value: \<empty>

- **PackagesProviderIconContentLoadMode**</br>
Determines if and from where the icon should be loaded. [Here](#content-load-mode), you will find the supported values.</br>
Default value: `None`

- **PackagesProviderLicenseContentLoadMode**</br>
Determines if and from where the license details should be loaded. [Here](#content-load-mode), you will find the supported values.</br>
Default value: `None`

- **PackagesProviderReadMeContentLoadMode**</br>
Determines if and from where the ReadMe should be loaded. [Here](#content-load-mode), you will find the supported values.</br>
Default value: `None`

- **PackagesProviderReleaseNotesContentLoadMode**</br>
Determines if and from where the release notes should be loaded. [Here](#content-load-mode), you will find the supported values.</br>
Default value: `None`</br>

### Project Items

- **AllowedLicense**</br>
A list of all allowed licenses or license expressions.</br>

- **AllowedLicenseByOwner**</br>
A list of all explicitly allowed package owners to be excluded from the license check.</br>
Please note that you need to map the owners manually as they cannot be automatically extracted.</br>
Default values: `Microsoft`

- **AllowedLicenseByPackage**</br>
A list of all explicitly allowed packages to be excluded from the license check.</br>
Default values: `MIT` `Apache-2.0`

- **KnownPackageId**</br>
Allows registering known package ID prefixes along with their corresponding owners and products.
For each package, the last matching prefix is used to determine the associated data.</br>
Default values: </br>
  - **System** | Owner: Microsoft, Product: .NET
  - **runtime** | Owner: Microsoft, Product: .NET Runtime
  - **NETStandard** | Owner: Microsoft, Product: .NET Standard
  - **Microsoft** | Owner: Microsoft
  - **Microsoft.Maui** | Owner: Microsoft, Product: MAUI
  - **Microsoft.Azure** | Owner: Microsoft, Product: Azure
  - **Microsoft.AspNetCore** | Owner: Microsoft, Product: ASP.NET Core
  - **Microsoft.EntityFrameworkCore** | Owner: Microsoft, Product: Entity Framework

## .NET Tool

The .NET Tool uses the same configurations as the Analyzer, plus the following:

### Project Properties

- **PackagesProviderOutputPath**</br>
Specifies the name or path to the target directory.</br>
Default value: `PackagesProvider`

- **PackagesProviderContentWriteMode**</br>
Specifies how the package content should be written. [Here](#content-write-mode), you will find the supported values.</br>
Default value: `Embed`</br>

- **PackagesProviderGenerateProjectFile**</br>
Determines whether a props file should be created to register all generated files (`true`) or not (`false`).</br>
Default value: `true`

- **PackagesProviderDownloadCacheFolder**</br>
Specifies the folder path where the downloaded contents should be cached.</br>
Default value: `%temp%/PackScan.PackagesProvider.Writer/DownloadCache`

### CLI Parameters

- **-p, --project-path** | The path to the project folder or project file. Default value: The current working directory
- **-l, --language** | Sets the programming language to be used. Default value: The language based on the project file extension.
- **--clear-output** | Determines whether the output folder should be emptied before execution (true) or not (false). Default value: `false`
- **-o, --output** | Overrides the project property **PackagesProviderOutputPath**.
- **--public** | Overrides the project property **PackagesProviderGeneratePublic**.
- **-class, --class-name** | Overrides the project property **PackagesProviderGenerateClassName**.
- **--namespace** | Overrides the project property **PackagesProviderGenerateNamespace**.
- **--nullable** | Overrides the project property **PackagesProviderNullableAnnotations**.
- **-parallel, --load-parallel** | Overrides the project property **PackagesProviderLoadContentsParallel**.
- **--content-write-mode** | Overrides the project property **PackagesProviderContentWriteMode**.
- **--add-project-file** | Overrides the project property **PackagesProviderGenerateProjectFile**.
- **--load-mode** | Overwrites the default values of the following load-mode values. [Here](#content-load-mode), you will find the supported values. Default value: \<empty>
- **--icon-load-mode** | Overrides the project property **PackagesProviderIconContentLoadMode**.
- **--license-load-mode** | Overrides the project property **PackagesProviderLicenseContentLoadMode**.
- **--readme-load-mode** | Overrides the project property **PackagesProviderReadMeContentLoadMode**.
- **--release-notes-load-mode** | Overrides the project property **PackagesProviderReleaseNotesContentLoadMode**.
- **--download-cache-folder** | Overrides the project property **PackagesProviderDownloadCacheFolder**.

## Enums

### Content Write Mode

The package data contains references to various files or websites whose content can be loaded.
To specify how the loaded content should be provided, you can choose from the following options:

- `Embed`: Treat the files as embedded resources.
- `InCode`: Write the content of the files in code and compile them.
- `File`: Copy the files to the output directory and read them from there.

### Content Load Mode

The package data contains references to various files or websites whose contents can be loaded.
However, loading the data can be slow. You can specify how the data should be loaded using the following values:

- `None`: Do not load any data.
- `PreferUrl`: Preferentially load the website content if a URL is specified. If no URL is specified, search for a file.
- `PreferFile`: Preferentially load the file content if a file is specified. If no file is specified, search for a URL.
- `OnlyUrl`: Load only the content of the file. Do not load website content.
- `OnlyFile`: Load only the content of the website. Do not load file content.
- `FileIfHasNoUrl`: Load the content only from a URL if no file was provided in the package.
- `UrlIfHasNoFile`: Load the content only from a file if no URL was provided in the package.

# Future?

Currently, the project only supports C#. However, it is designed in a way that allows for future support of VB.NET or F#.
The ability to read package data or generate code as a separate package is not currently available, but the project is designed with the flexibility to release separate packages for these functionalities in the future.

If you have any feature requests or ideas for additional tools, code generators, or analyzers, please feel free to share them. Your feedback and suggestions are important.
You can also contribute by creating a pull request or sharing any further requests or suggestions.

I value your input and appreciate your involvement in shaping the project.
Thank you for your support!

# Contributing

Contributions are welcome! If you have any suggestions, ideas, or bug reports, please open an issue.

If you would like to contribute code, please fork the repository and create a pull request with your changes.

# ChangeLog

## [v0.2.0 - 2023-11-01](https://github.com/loop8ack/PackScan/releases/tag/v0.2.0)
### Added
- New configuration option allowing content to be loaded from files only if no URL is provided and vice versa.

## [v0.1.1 - 2023-10-31](https://github.com/loop8ack/PackScan/releases/tag/v0.1.1)
### Added
- Initial release of PackScan.

# License

This project is licensed under the [MIT License](LICENSE).
