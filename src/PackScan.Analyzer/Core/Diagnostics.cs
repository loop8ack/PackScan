using System.Collections.Immutable;
using System.Drawing;

using Microsoft.CodeAnalysis;

namespace PackScan.Analyzer.Core;

internal static class Diagnostics
{
    private const string Prefix = "L8PPA";
    public const string Category = "PackagesProvider Analyzer";

    private enum Id
    {
        // Options
        OptionNotFound = 100,
        OptionNotParsedEnum,
        OptionNoParsedBool,
        OptionNoParsedValue,
        OptionNoParsedSize,

        // Licenses Analyzer
        LicenseNotAllowed = 200,
        LicenseValidationWithOperatorNotSupported,
        EmptyLicenseIsNotAllowed,
    }

    public static ImmutableArray<DiagnosticDescriptor> AllDescriptors { get; }
        = ImmutableArray.CreateRange(GetDiagnostics());

    private static IEnumerable<DiagnosticDescriptor> GetDiagnostics()
    {
        // Options
        yield return OptionNotFound.Descriptor;
        yield return OptionNotParsedEnum.Descriptor;
        yield return OptionNotParsedBool.Descriptor;
        yield return OptionNotParsedValue.Descriptor;

        // Licenses Analyzer
        yield return LicenseNotAllowed.Descriptor;
        yield return LicenseValidationWithOperatorNotSupported.Descriptor;
        yield return EmptyLicenseIsNotAllowed.Descriptor;
    }

    public static class OptionNotFound
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.OptionNotFound:000}",
                title: "Not found option value",
                messageFormat: "Could not find required option '{0}'.",
                category: Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static Diagnostic Create(string optionName)
        {
            return Diagnostic.Create(Descriptor, Location.None, optionName);
        }
    }

    public static class OptionNotParsedEnum
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.OptionNotParsedEnum:000}",
                title: "Not supported enum value",
                messageFormat: "Could not parse '{0}' value '{1}' to '{2}'. Supported values: {3}",
                category: Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static Diagnostic Create<TEnum>(string optionName, string value)
            where TEnum : struct, Enum
        {
            string enumNames = string.Join(", ", Enum.GetNames(typeof(TEnum)));

            return Diagnostic.Create(Descriptor, Location.None, optionName, value, typeof(TEnum).Name, enumNames);
        }
    }

    public static class OptionNotParsedBool
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.OptionNoParsedBool:000}",
                title: "Not supported boolean value",
                messageFormat: "Could not parse '{0}' value '{1}' as boolean. Supported values: true, false",
                category: Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static Diagnostic Create(string optionName, string value)
        {
            return Diagnostic.Create(Descriptor, Location.None, optionName, value);
        }
    }

    public static class OptionNotParsedValue
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.OptionNoParsedValue:000}",
                title: "Not supported value",
                messageFormat: "Could not parse '{0}' value '{1}' as {2}. Supported values: {3}",
                category: Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static Diagnostic Create(string optionName, string value, string typeName, IEnumerable<string> supportedValues)
        {
            return Diagnostic.Create(Descriptor, Location.None, optionName, value, typeName, string.Join(", ", supportedValues));
        }
    }

    public static class OptionNotParsedSize
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.OptionNoParsedSize:000}",
                title: "Not supported value",
                messageFormat: "Could not parse '{0}' value '{1}' as size. Supported format: <width>x<height>",
                category: Category,
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

        public static Diagnostic Create(string optionName, string value)
        {
            return Diagnostic.Create(Descriptor, Location.None, optionName, value);
        }
    }

    public static class LicenseNotAllowed
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.LicenseNotAllowed:000}",
                title: "License is not allowed",
                messageFormat: "The license '{0}' is not allowed. Affected packages: {1}",
                category: Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static Diagnostic Create(string license, string formattedPackageIds)
        {
            return Diagnostic.Create(Descriptor, Location.None, license, formattedPackageIds);
        }
    }

    public static class LicenseValidationWithOperatorNotSupported
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.LicenseValidationWithOperatorNotSupported:000}",
                title: "License WITH operator is not supported",
                messageFormat: "The license '{0}' uses the WITH operator, which is not supported. Consider allowing the license expression (AllowedLicense) or the package (AllowedLicenseByPackage) directly. Affected packages: {1}",
                category: Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static Diagnostic Create(string license, string formattedPackageIds)
        {
            return Diagnostic.Create(Descriptor, Location.None, license, formattedPackageIds);
        }
    }

    public static class EmptyLicenseIsNotAllowed
    {
        public static DiagnosticDescriptor Descriptor { get; }
            = new DiagnosticDescriptor(
                id: $"{Prefix}{(int)Id.EmptyLicenseIsNotAllowed:000}",
                title: "Packages without a license are not allowed",
                messageFormat: "Empty licenses are not allowed. Possibly the license could not be read. Consider allowing the package directly (AllowedLicenseByPackage) or empty licenses (AllowEmptyLicenses). Affected packages: {0}",
                category: Category,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        public static Diagnostic Create(string formattedPackageIds)
        {
            return Diagnostic.Create(Descriptor, Location.None, formattedPackageIds);
        }
    }
}
