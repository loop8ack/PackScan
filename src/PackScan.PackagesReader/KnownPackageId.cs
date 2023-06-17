namespace PackScan.PackagesReader;

/// <summary>
/// Represents a known package ID prefix and specifies the associated owner or product for the package.
/// </summary>
public record class KnownPackageId(string IdPrefix, string? Owner, string? Product);
