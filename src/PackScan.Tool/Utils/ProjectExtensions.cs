using System.Diagnostics.CodeAnalysis;

using Microsoft.Build.Evaluation;

namespace PackScan.Tool.Utils;

internal static class ProjectExtensions
{
    public static bool TryGetPropertyValue(this Project project, string propertyName, [MaybeNullWhen(false)] out string value)
    {
        ProjectProperty? property = project.GetProperty(propertyName);

        if (property is null)
        {
            value = default;
            return false;
        }

        value = property.EvaluatedValue;
        return true;
    }
}
