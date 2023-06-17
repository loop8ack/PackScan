using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

using PackScan.PackagesProvider.Generator.Utils;

using Stream = System.IO.Stream;

namespace PackScan.PackagesProvider.Generator.Code.Documentation;

internal sealed class DocumentationReader : IDocumentationReader
{
    private static readonly Regex _crefDocElementRegex = new Regex(@"<(?<element>[^\s]+)\s+cref=""(?<type>\w):(?<member>[^""]+)""\s*\/>", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _documentations = new();
    private readonly INamespaceProvider _nsProvider;

    public DocumentationReader(INamespaceProvider nsProvider)
    {
        _nsProvider = nsProvider;
    }

    public void ReadDocumentations(CancellationToken cancellationToken)
    {
        Assembly assembly = typeof(DocumentationReader).Assembly;

        foreach (string? name in assembly.GetManifestResourceNames())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (name.StartsWith("Documentation.") && name.EndsWith(".xml"))
            {
                using Stream? stream = assembly.GetManifestResourceStream(name);

                if (stream is null)
                    continue;

                using XmlReader reader = XmlReader.Create(stream);

                ReadDocumentationFile(reader, cancellationToken);
            }
        }
    }
    private void ReadDocumentationFile(XmlReader reader, CancellationToken cancellationToken)
    {
        Dictionary<string, string> byMember = new();
        string? assembly = null;

        while (reader.Read())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (reader.NodeType != XmlNodeType.Element)
                continue;

            switch (reader.Name)
            {
                case "assembly":
                    assembly = TryReadAssemblyName(reader);
                    break;

                case "member":
                    TryReadMemberDocumentation(reader, byMember);
                    break;
            }
        }

        if (assembly is not null and { Length: > 0 } && byMember.Count > 0)
            _documentations.Add(assembly, byMember);
    }
    private static string? TryReadAssemblyName(XmlReader reader)
    {
        bool isInName = false;

        while (reader.Read())
        {
            if (isInName && reader.NodeType == XmlNodeType.Text)
                return reader.Value;

            isInName = reader.NodeType == XmlNodeType.Element
                && reader.Name == "name";
        }

        return null;
    }
    private void TryReadMemberDocumentation(XmlReader reader, IDictionary<string, string> byMember)
    {
        bool hasName = reader.MoveToAttribute("name") && reader.ReadAttributeValue();

        if (!hasName)
            return;

        string name = reader.Value;

        if (!reader.MoveToElement())
            return;

        string doc = reader.ReadInnerXml();
        doc = NormalizeDocumentation(doc);
        byMember.Add(name, doc);
    }
    private string NormalizeDocumentation(string s)
    {
        string[] lines = s.SplitByLine(StringSplitOptions.None);
        int jointIndent = int.MaxValue;
        int skip = 0;
        int take = 0;

        foreach (string line in lines)
        {
            int indent = CountLeadingWhitespace(line);

            if (indent == line.Length)
            {
                if (take == 0)
                    skip++;

                continue;
            }

            jointIndent = Math.Min(jointIndent, indent);
            take++;
        }

        lines = lines
            .Skip(skip)
            .Take(take)
            .Select(l => l.Substring(jointIndent))
            .ToArray();

        for (int i = 0; i < lines.Length; i++)
            lines[i] = ReplaceCrefNamespacesForWrite(lines[i]);

        return string.Join(Environment.NewLine, lines);
    }
    private static int CountLeadingWhitespace(string s)
    {
        int result = 0;

        for (int i = 0; i < s.Length; i++)
        {
            if (s[i] == ' ')
                result++;
            else
                break;
        }

        return result;
    }
    private string ReplaceCrefNamespacesForWrite(string line)
    {
        return _crefDocElementRegex.Replace(line, m =>
        {
            string element = m.Groups["element"].Value;
            string type = m.Groups["type"].Value;
            string cref = m.Groups["member"].Value;

            int nsEndIndex = cref.LastIndexOf('.');
            int dotCount = 0;

            foreach (char c in line)
            {
                if (c is '.')
                    dotCount++;
            }

            if (type != Token.Type)
                nsEndIndex = cref.LastIndexOf('.', nsEndIndex - 1);

            string? ns = cref.Substring(0, nsEndIndex);
            string name = cref.Substring(nsEndIndex + 1);

            if (_nsProvider.TryGetForWrite(@ns, out ns))
                ns += ".";

            return $"""<{element} cref="{@ns}{name}" /> """;
        });
    }

    public string? TryGet(MemberInfo member)
    {
        Assembly? assembly = member.DeclaringType?.Assembly;
        string? declaringName = member.DeclaringType?.FullName;
        string memberName = member.Name;

        switch (member)
        {
            case Type type:
                assembly = type.Assembly;
                declaringName = type.Namespace;
                break;

            case MethodInfo method:
                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length > 0)
                    memberName += $"({string.Join(", ", parameters.Select(p => p.ParameterType.FullName))})";

                break;
        }

        if (assembly is null)
            throw new NotSupportedException($"The member type '{member.GetType().Name}' is not supported");

        return TryGet(assembly, member.MemberType, declaringName, memberName);
    }

    public string? TryGet(Type declaringType, MemberTypes memberType, string memberName)
        => TryGet(declaringType.Assembly, memberType, declaringType.FullName, memberName);

    private string? TryGet(Assembly assembly, MemberTypes memberType, string? declaringName, string memberName)
    {
        if (!string.IsNullOrEmpty(declaringName))
            memberName = $"{declaringName}.{memberName}";

        switch (memberType)
        {
            case MemberTypes.TypeInfo:
                return TryGet(assembly, $"{Token.Type}:{memberName}");

            case MemberTypes.Field:
                return TryGet(assembly, $"{Token.Field}:{memberName}");

            case MemberTypes.Method:
                return TryGet(assembly, $"{Token.Method}:{memberName}");

            case MemberTypes.Property:
                return TryGet(assembly, $"{Token.Property}:{memberName}");

            default:
                throw new NotSupportedException($"The member type '{memberType}' is not supported");
        }
    }

    private string? TryGet(Assembly assembly, string memberName)
    {
        string? assemblyName = assembly.GetName()?.Name;

        if (assemblyName is null)
            return null;

        if (_documentations.TryGetValue(assemblyName, out IReadOnlyDictionary<string, string>? byMember)
            && byMember.TryGetValue(memberName, out string? documentation))
            return documentation;

        return null;
    }

    private static class Token
    {
        public const string Type = "T";
        public const string Field = "F";
        public const string Method = "M";
        public const string Property = "P";
    }
}
