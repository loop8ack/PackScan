using System.Diagnostics.CodeAnalysis;

using PackScan.PackagesReader.Abstractions;

namespace PackScan.PackagesProvider.Generator.Utils;

internal sealed class PackageContentDataEqualityComparer : IEqualityComparer<IPackageContentData>
{
    public static PackageContentDataEqualityComparer Instance { get; } = new();

    public bool Equals(IPackageContentData? x, IPackageContentData? y)
    {
        if (x is null && y is null)
            return true;

        if (x is null || y is null)
            return false;

        if (ReferenceEquals(x, y))
            return true;

        return EqualityComparer<Uri?>.Default.Equals(x.Url, y.Url)
            && EqualityComparer<string?>.Default.Equals(x.FilePath, y.FilePath);
    }

    public int GetHashCode([DisallowNull] IPackageContentData data)
    {
        if (data is null)
            return -1;

        return HashCode.Combine(data.Url, data.FilePath);
    }
}
