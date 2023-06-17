namespace PackScan.PackagesProvider.Generator.Info;

public sealed class ProductInfo
{
    public required string? Author { get; init; }
    public required string? Version { get; init; }
    public required string? Title { get; init; }
    public required string? RepositoryUrl { get; init; }
}
