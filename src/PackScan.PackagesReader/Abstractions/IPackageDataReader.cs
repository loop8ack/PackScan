namespace PackScan.PackagesReader.Abstractions;

public interface IPackageDataReader
{
    IReadOnlyCollection<IPackageData> Read(CancellationToken cancellationToken = default);
}
