using PackScan.PackagesProvider;

namespace PackScan.Analyzer.Test;

public class IntegrationTests
{
    [Fact]
    public void Exists_Moq()
    {
        IPackage? package = Packages.GetPackageById("Moq");
      
        Assert.NotNull(package);
        Assert.Same(package.Id, "Moq");
        Assert.Equal(package.Version.Version, new Version(4, 18, 4));
        Assert.True(package.IsProjectDependency);
        Assert.Contains(package.DependencyPackages, p => p.Id == "Castle.Core");
    }

    [Fact]
    public void Exists_Castle_Core()
    {
        IPackage? package = Packages.GetPackageById("Castle.Core");

        Assert.NotNull(package);
        Assert.Same(package.Id, "Castle.Core");
        Assert.Equal(package.Version.Version, new Version(5, 1, 1));
        Assert.False(package.IsProjectDependency);
    }
}
