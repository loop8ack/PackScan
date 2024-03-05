using System.Text;

using static CodegenCS.Symbols;

using MemoryStream = System.IO.MemoryStream;

namespace PackScan.PackagesProvider.Generator.Code.CSharp;

internal sealed class FormattableStringFactory
{
    private static readonly IReadOnlyDictionary<Type, string> _predefinedTypeNames
        = new Dictionary<Type, string>
        {
            [typeof(bool)] = "bool",
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(char)] = "char",
            [typeof(decimal)] = "decimal",
            [typeof(double)] = "double",
            [typeof(float)] = "float",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(nint)] = "nint",
            [typeof(nuint)] = "nuint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(object)] = "object",
            [typeof(string)] = "string",
        };

    private readonly StringBuilder _tmpStringBuilder = new();
    private readonly INamespaceProvider _namespaceProvider;

    public FormattableString Null { get; } = $"null";
    public FormattableString True { get; } = $"true";
    public FormattableString False { get; } = $"false";

    public FormattableStringFactory(INamespaceProvider namespaceProvider)
        => _namespaceProvider = namespaceProvider;

    public string Type<T>()
        => Type(typeof(T));
    public string Type(Type type)
    {
        AppendType(_tmpStringBuilder, type);
        string result = _tmpStringBuilder.ToString();
        _tmpStringBuilder.Clear();
        return result;
    }
    private void AppendType(StringBuilder sb, Type type)
    {
        Type[] genericArguments = type.GetGenericArguments();
        bool isArray = false;
        bool nullable = false;

        if (type.IsArray)
        {
            type = type.GetElementType()!;
            isArray = true;
        }
        else
        {
            Type? underlyingType = Nullable.GetUnderlyingType(type);

            if (underlyingType is not null)
            {
                type = underlyingType;
                nullable = true;
            }
        }

        if (_predefinedTypeNames.TryGetValue(type, out string? typeName))
            sb.Append(typeName);
        else
        {
            sb.Append("global::");

            if (_namespaceProvider.TryGetForWrite(type.Namespace, out string? ns))
            {
                sb.Append(ns);
                sb.Append('.');
            }

            typeName = type.Name;

            int genericMarkerIndex = typeName.IndexOf('`');

            if (genericMarkerIndex >= 0)
                typeName = typeName.Substring(0, genericMarkerIndex);

            sb.Append(typeName);
        }

        if (nullable)
            sb.Append('?');

        if (isArray)
            sb.Append("[]");

        if (genericArguments.Length > 0)
        {
            sb.Append('<');

            for (int i = 0; i < genericArguments.Length; i++)
            {
                if (i > 0)
                    sb.Append(", ");

                AppendType(sb, genericArguments[i]);
            }

            sb.Append('>');
        }
    }

    public FormattableString Bool(bool value)
         => value ? True : False;

    public FormattableString String(string? text)
    {
        if (text is null)
            return Null;

        bool hasLineBreaks = text.Contains('\r')
            || text.Contains('\n');

        if (!hasLineBreaks)
        {
            text = text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");

            return $"""
                    "{text}"
                    """;
        }

        text = text.Replace("\"", "\"\"");

        return $"""
                @"{RAW(text)}"
                """;
    }

    public FormattableString Uri(Uri? uri)
    {
        if (uri is null)
            return Null;

        UriKind kind = uri.IsAbsoluteUri
            ? UriKind.Absolute
            : UriKind.Relative;

        return $"new {Type<Uri>()}({String(uri.OriginalString)}, {Enum(kind)})";
    }

    public FormattableString Version(Version? version)
    {
        if (version is null)
            return Null;

        string? revision = version.Revision <= 0 ? null : $", {version.Revision}";
        string? build = version.Build <= 0 && revision is null ? null : $", {version.Build}";
        string? minor = $", {version.Minor}";
        int major = version.Major;

        return $"new {Type<Version>()}({major}{minor}{build}{revision})";
    }

    public FormattableString Strings(IEnumerable<string> strings)
    {
        bool isFirst = true;

        foreach (string s in strings)
        {
            if (isFirst)
                isFirst = false;
            else
                _tmpStringBuilder.Append(", ");

            _tmpStringBuilder.Append(String(s));
        }

        if (isFirst)
            return EmptyArray<string>();

        // In a FormattableString, StringBuilder.ToString() is called only when the final result is built
        string tmpString = _tmpStringBuilder.ToString();

        FormattableString result = $"new string[] {{ {tmpString} }}";

        _tmpStringBuilder.Clear();

        return result;
    }

    public FormattableString Enum<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        if (_namespaceProvider.TryGetForWrite(typeof(TEnum).Namespace, out string? ns))
            ns += ".";

        return $"{ns}{typeof(TEnum).Name}.{value}";
    }

    public FormattableString ByteArray(byte[] bytes)
    {
        if (bytes.Length == 0)
            return EmptyArray<byte>();

        return $"new byte[] {{ {string.Join(", ", bytes)} }}";
    }

    public FormattableString EmptyArray<T>()
    {
        return $"{Type<Array>()}.Empty<{Type<T>()}>()";
    }

    public FormattableString EmptyStream()
    {
        return $$"""
            new {{Type<MemoryStream>()}}({{EmptyArray<byte>()}}, writable: false)
            """;
    }
}
