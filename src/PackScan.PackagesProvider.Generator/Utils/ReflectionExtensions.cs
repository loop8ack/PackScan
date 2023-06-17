using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PackScan.PackagesProvider.Generator.Utils;

internal static class ReflectionExtensions
{
    public static bool IsNullable(this PropertyInfo property)
        => IsNullableHelper(property.PropertyType, property.DeclaringType, property.CustomAttributes);

    [SuppressMessage("Style", "IDE0008:Use explicit type")]
    private static bool IsNullableHelper(Type memberType, MemberInfo? declaringType, IEnumerable<CustomAttributeData> customAttributes)
    {
        // Source: https://stackoverflow.com/questions/58453972/how-to-use-net-reflection-to-check-for-nullable-reference-type#answer-58454489

        if (memberType.IsValueType)
            return Nullable.GetUnderlyingType(memberType) != null;

        CustomAttributeData? nullable = customAttributes
            .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableAttribute");

        if (nullable?.ConstructorArguments.Count == 1)
        {
            CustomAttributeTypedArgument attributeArgument = nullable.ConstructorArguments[0];

            if (attributeArgument.ArgumentType == typeof(byte[]))
            {
                var args = (ReadOnlyCollection<CustomAttributeTypedArgument>)attributeArgument.Value!;

                if (args.Count > 0 && args[0].ArgumentType == typeof(byte))
                {
                    return (byte)args[0].Value! == 2;
                }
            }
            else if (attributeArgument.ArgumentType == typeof(byte))
            {
                return (byte)attributeArgument.Value! == 2;
            }
        }

        for (MemberInfo? type = declaringType; type != null; type = type.DeclaringType)
        {
            CustomAttributeData? context = type.CustomAttributes
                .FirstOrDefault(x => x.AttributeType.FullName == "System.Runtime.CompilerServices.NullableContextAttribute");

            if (context?.ConstructorArguments.Count == 1
                && context.ConstructorArguments[0].ArgumentType == typeof(byte))
            {
                return (byte)context.ConstructorArguments[0].Value! == 2;
            }
        }

        // Couldn't find a suitable attribute
        return false;
    }
}
