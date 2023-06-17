using System.CommandLine;

using Microsoft.Build.Locator;

using PackScan.Tool.PackagesProvider;

MSBuildLocator.RegisterDefaults();

new RootCommand("This tool offers to read and process NuGet package data.")
{
    GeneratePackagesProviderCommand.Create(),
}.Invoke(args);
