using System.Reflection;

namespace PackScan.PackagesProvider.Generator.Code.Documentation;

internal interface IDocumentationReader
{
    void ReadDocumentations(CancellationToken cancellationToken);
    string? TryGet(MemberInfo member);
    string? TryGet(Type declaringType, MemberTypes memberType, string memberName);
}
