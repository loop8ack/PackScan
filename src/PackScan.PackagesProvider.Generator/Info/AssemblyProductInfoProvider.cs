using System.Reflection;

namespace PackScan.PackagesProvider.Generator.Info;

public sealed class AssemblyProductInfoProvider : IProductInfoProvider
{
    private readonly Assembly _assembly;

    public AssemblyProductInfoProvider(Assembly assembly)
        => _assembly = assembly;

    public ProductInfo GetProductInfo()
    {
        return new ProductInfo()
        {
            Author = _assembly
                .GetCustomAttribute<AssemblyCompanyAttribute>()
                ?.Company,

            Version = _assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion,

            Title = _assembly
                .GetCustomAttribute<AssemblyTitleAttribute>()
                ?.Title,

            RepositoryUrl = _assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(x => x.Key == "RepositoryUrl")
                ?.Value
        };
    }
}
