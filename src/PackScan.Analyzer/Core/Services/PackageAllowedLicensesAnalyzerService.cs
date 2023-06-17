using System.Diagnostics;
using System.Text;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

using NuGet.Packaging.Licenses;

using PackScan.Analyzer.Core.Options;
using PackScan.PackagesReader.Abstractions;

namespace PackScan.Analyzer.Core.Services;

internal record class PackageAllowedLicensesAnalyzerService
{
    private readonly PackageDataReaderService _readerService;

    private OptionValue<bool> IsEnabled { get; }
    private OptionValue<bool> AnalyzeDependencies { get; }
    private OptionValue<bool> AllowEmpty { get; }
    private OptionValue<IReadOnlyList<string>> AllowedLicensesByPackage { get; }
    private OptionValue<IReadOnlyList<string>> AllowedLicensesByOwner { get; }
    private OptionValue<IReadOnlyList<string>> AllowedLicenses { get; }

    public PackageAllowedLicensesAnalyzerService(AnalyzerConfigOptions options)
    {
        _readerService = new(options);

        IsEnabled = options.GetOptionBool("AllowedLicensesAnalyzationEnabled");
        AnalyzeDependencies = options.GetOptionBool("AnalyzePackageDependencyLicenses");
        AllowEmpty = options.GetOptionBool("AllowEmptyLicenses");
        AllowedLicensesByPackage = options.GetOptionStringList("_AllowedLicensesByPackage");
        AllowedLicensesByOwner = options.GetOptionStringList("_AllowedLicensesByOwner");
        AllowedLicenses = options.GetOptionStringList("_AllowedLicense");
    }

    public void AnalyzeLicenses(CompilationAnalysisContext context)
    {
        if (!IsEnabled)
            return;

        IReadOnlyList<Diagnostic> diagnostics = ValidateOptions();

        if (diagnostics.Count > 0)
        {
            foreach (Diagnostic diagnostic in diagnostics)
                context.ReportDiagnostic(diagnostic);

            return;
        }

        foreach (Diagnostic diagnostic in Analyze(context.CancellationToken))
            context.ReportDiagnostic(diagnostic);

        return;
    }

    private IReadOnlyList<Diagnostic> ValidateOptions()
    {
        List<Diagnostic> diagnostics = new();

        _readerService.ValidateOptions(diagnostics);

        IsEnabled.Validate(diagnostics);
        AnalyzeDependencies.Validate(diagnostics);
        AllowEmpty.Validate(diagnostics);
        AllowedLicensesByPackage.Validate(diagnostics);
        AllowedLicensesByOwner.Validate(diagnostics);
        AllowedLicenses.Validate(diagnostics);

        return diagnostics;
    }

    private IEnumerable<Diagnostic> Analyze(CancellationToken cancellationToken)
    {
        Dictionary<string, IPackageData> packages = _readerService.Read().ToDictionary(x => x.Id, x => x);

        Dictionary<string, AnalyzeLicenseResult> resultsByLicense = new();
        HashSet<string> packageIdsWithoutLicense = new();

        foreach (IPackageData rootPackage in packages.Values)
        {
            cancellationToken.ThrowIfCancellationRequested();

            AnalyzePackageLicense(resultsByLicense, packageIdsWithoutLicense, rootPackage, out bool skipHierarchy);

            if (!skipHierarchy && AnalyzeDependencies)
            {
                foreach (string childPackageId in rootPackage.DependencyPackageIds)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    AnalyzePackageLicense(resultsByLicense, packageIdsWithoutLicense, packages[childPackageId], out _);
                }
            }
        }

        foreach ((string license, AnalyzeLicenseResult result) in resultsByLicense.Select(x => (x.Key, x.Value)))
        {
            if (!result.IsAllowed)
                yield return Diagnostics.LicenseNotAllowed.Create(license, CreateFormattedPackageIdsTree(packages, result.PackageIds));

            if (result.ContainsWithOperator)
                yield return Diagnostics.LicenseValidationWithOperatorNotSupported.Create(license, CreateFormattedPackageIdsTree(packages, result.PackageIds));
        }

        if (packageIdsWithoutLicense.Count > 0)
            yield return Diagnostics.EmptyLicenseIsNotAllowed.Create(CreateFormattedPackageIdsTree(packages, packageIdsWithoutLicense));
    }

    private string CreateFormattedPackageIdsTree(IReadOnlyDictionary<string, IPackageData> packagesById, IEnumerable<string> outputPackageIds)
    {
        StringBuilder sb = new();

        foreach (IPackageData package in outputPackageIds.Select(id => packagesById[id]))
        {
            sb.Append(" - ");
            sb.Append(package.Id);

            int count = 0;

            foreach (IPackageData parentPackage in FindDirectlyReferencedParentPackages(packagesById, package))
            {
                if (count++ > 0)
                    sb.Append(", ");
                else
                    sb.Append(" (see: ");

                sb.Append(parentPackage.Id);
            }

            if (count > 0)
                sb.Append(")");
        }

        return sb.ToString();

        static IEnumerable<IPackageData> FindDirectlyReferencedParentPackages(IReadOnlyDictionary<string, IPackageData> packagesById, IPackageData logPackage)
        {
            foreach (IPackageData package in packagesById.Values)
            {
                if (!package.IsProjectDependency)
                    continue;

                if (package.Id == logPackage.Id)
                    continue;

                if (IsDependency(packagesById, package, logPackage))
                    yield return package;
            }
        }

        static bool IsDependency(IReadOnlyDictionary<string, IPackageData> packagesById, IPackageData parentPackage, IPackageData package)
        {
            if (parentPackage.DependencyPackageIds.Contains(package.Id))
                return true;

            foreach (IPackageData dependencyPackage in parentPackage.DependencyPackageIds.Select(id => packagesById[id]))
            {
                if (IsDependency(packagesById, dependencyPackage, package))
                    return true;
            }

            return false;
        }
    }

    private void AnalyzePackageLicense(IDictionary<string, AnalyzeLicenseResult> resultsByLicense, HashSet<string> packageIdsWithoutLicense, IPackageData package, out bool skipHierarchy)
    {
        skipHierarchy = false;

        if (AllowedLicensesByPackage.Value.Contains(package.Id))
        {
            skipHierarchy = true;
            return;
        }

        if (package.Owner is not null and { Length: > 0 } && AllowedLicensesByOwner.Value.Contains(package.Owner))
        {
            skipHierarchy = true;
            return;
        }

        string? license = package.License?.Expression;

        if (license is null or { Length: 0 })
        {
            if (!AllowEmpty)
                packageIdsWithoutLicense.Add(package.Id);

            return;
        }

        if (!resultsByLicense.TryGetValue(license, out AnalyzeLicenseResult? result))
        {
            result = AnalyzeLicense(license);
            resultsByLicense.Add(license, result);
        }

        result.PackageIds.Add(package.Id);
    }

    private AnalyzeLicenseResult AnalyzeLicense(string license)
    {
        if (AllowedLicenses.Value.Contains(license))
            return new AnalyzeLicenseResult(license, isAllowed: true);

        NuGetLicenseExpression expression;

        try
        {
            expression = NuGetLicenseExpression.Parse(license);
        }
        catch
        {
            return new AnalyzeLicenseResult(license, isAllowed: false);
        }

        return AnalyzeLicenseExpression(expression, license);
    }

    private AnalyzeLicenseResult AnalyzeLicenseExpression(NuGetLicenseExpression expression, string expressionString)
    {
        bool containsWithOperator = false;
        bool isAllowed = IsAllowed(expression, ref containsWithOperator);

        return new AnalyzeLicenseResult(expressionString, isAllowed, containsWithOperator);

        bool IsAllowed(NuGetLicenseExpression expression, ref bool containsWithOperator)
        {
            switch (expression.Type)
            {
                case LicenseExpressionType.License:
                    return expression is NuGetLicense license
                        && AllowedLicenses.Value.Contains(license.Identifier);

                case LicenseExpressionType.Operator:
                    return expression is LicenseOperator licenseOperator
                        && IsAllowedOperator(licenseOperator, ref containsWithOperator);

                default:
                    return false;
            }
        }

        bool IsAllowedOperator(LicenseOperator licenseOperator, ref bool containsWithOperator)
        {
            containsWithOperator = false;

            switch (licenseOperator.OperatorType)
            {
                case LicenseOperatorType.LogicalOperator:
                    return licenseOperator is LogicalOperator logicalOperator
                        && IsAllowedLogicalOperator(logicalOperator, ref containsWithOperator);

                case LicenseOperatorType.WithOperator:
                    return licenseOperator is WithOperator withOperator
                        && IsAllowedWithOperator(withOperator, ref containsWithOperator);

                default:
                    return false;
            }
        }

        bool IsAllowedLogicalOperator(LogicalOperator logicalOperator, ref bool containsWithOperator)
        {
            switch (logicalOperator.LogicalOperatorType)
            {
                case LogicalOperatorType.And:
                    return IsAllowed(logicalOperator.Left, ref containsWithOperator)
                        && IsAllowed(logicalOperator.Right, ref containsWithOperator);

                case LogicalOperatorType.Or:
                    return IsAllowed(logicalOperator.Left, ref containsWithOperator)
                        || IsAllowed(logicalOperator.Right, ref containsWithOperator);

                default:
                    return false;
            }
        }

        bool IsAllowedWithOperator(WithOperator withOperator, ref bool containsWithOperator)
        {
            containsWithOperator = true;

            return IsAllowed(withOperator.License, ref containsWithOperator);
        }
    }

    private sealed class AnalyzeLicenseResult
    {
        public string License { get; }
        public bool IsAllowed { get; }
        public bool ContainsWithOperator { get; }
        public HashSet<string> PackageIds { get; } = new();

        public AnalyzeLicenseResult(string license, bool isAllowed, bool containsWithOperator = false)
        {
            License = license;
            IsAllowed = isAllowed;
            ContainsWithOperator = containsWithOperator;
        }
    }
}
