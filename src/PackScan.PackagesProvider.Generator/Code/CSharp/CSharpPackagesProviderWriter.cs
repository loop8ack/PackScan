using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using CodegenCS;

using PackScan.PackagesProvider.Generator.Code.Documentation;
using PackScan.PackagesProvider.Generator.PackageContents;
using PackScan.PackagesProvider.Generator.Utils;
using PackScan.PackagesReader.Abstractions;

using static CodegenCS.Symbols;

using File = System.IO.File;
using MemoryStream = System.IO.MemoryStream;
using Stream = System.IO.Stream;
using StreamReader = System.IO.StreamReader;

namespace PackScan.PackagesProvider.Generator.Code.CSharp;

internal sealed class CSharpPackagesProviderWriter : CSharpPackagesProviderWriterBase
{
    private const string Icon = "Icon";
    private const string License = "License";
    private const string ReadMe = "ReadMe";
    private const string ReleaseNotes = "ReleaseNotes";

    private static class ContentMethods
    {
        public const string ReadText = "ReadText";
        public const string OpenImageStream = "OpenImageStream";
    }

    private IReadOnlyList<IPackageData> Packages => Options.Packages;
    private IReadOnlyDictionary<string, string> PropertyNamesByPackageId => Options.PropertyNamesByPackageId;

    public CSharpPackagesProviderWriter(IDocumentationReaderFactory docReaderFactory, PackagesProviderWriterOptions options)
        : base(docReaderFactory, options)
    {
    }

    protected override void AddCodeFiles(ICodegenContext codeContext)
    {
        codeContext[$"{ClassName}.g.cs"].Write(CreateClass());

        if (ContentWriteMode == ContentWriteMode.InCode)
            CreateContentClasses(codeContext);
    }

    protected override FormattableString CreatePackagesProviderProps()
    {
        return $"""
            <Project>
                <ItemGroup>
                    {CreateCompileItems()}
                    {CreateContentItems()}
                </ItemGroup>
            </Project>
            """;

        IEnumerable<FormattableString> CreateCompileItems()
        {
            yield return $"""
                <Compile Include="$(MSBuildThisFileDirectory)/{ClassName}.g.cs" />
                """;

            if (ContentWriteMode != ContentWriteMode.InCode)
                yield break;

            foreach (IPackageData package in Packages)
            {
                if (!TryGetAllPackageContent(package, out _, out _, out _, out _))
                    continue;

                yield return $"""
                    <Compile Include="$(MSBuildThisFileDirectory)/{GetContentClassFileName(package.Id)}" />
                    """;
            }
        }

        IEnumerable<FormattableString> CreateContentItems()
        {
            foreach (IPackageContent content in ContentProvider.AllContents)
            {
                switch (ContentWriteMode)
                {
                    case ContentWriteMode.Embed:
                        yield return $"""
                            <EmbeddedResource Include="$(MSBuildThisFileDirectory)/{content.File.Name}">
                                <LogicalName>{content.File.Name}</LogicalName>
                            </EmbeddedResource>
                            """;
                        break;

                    case ContentWriteMode.File:
                        yield return $"""
                            <None Include="$(MSBuildThisFileDirectory)/{content.File.Name}">
                                <CopyToOutputDirectory>Always</CopyToOutputDirectory>
                            </None>
                            """;
                        break;
                }
            }
        }
    }

    private FormattableString CreateClass()
    {
        return CreateFile(new()
        {
            BeforeNamespace = $$"""
                #pragma warning disable IDE0074 // Use compound assignment
                #pragma warning disable IDE0161 // Convert to file-scoped namespace
                """,

            InNamespace = $$"""
                {{TryGetDocumentation(typeof(IPackagesProvider))}}
                {{Access}} sealed partial class {{ClassName}} : {{Create.Type<IPackagesProvider>()}}
                {
                    {{CreateGetPackageByIdMethod()}}

                    {{CreateGetPackagesMethod()}}

                    {{Packages.Select(CreatePackageProperty)}}

                    {{CreateLoadMethods()}}

                {{TLW}}
                {{IF(NullableAnnotations)}}
                #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                {{ENDIF}}
                    {{TLW}}
                    {{CreateModelClass<IPackage>()}}
                    {{CreateModelClass<IPackageVersion>()}}
                    {{CreateModelClass<IPackageRepository>()}}
                    {{CreateModelClassWithImage<IPackageIcon>(nameof(IPackageIcon.OpenStream))}}
                    {{CreateModelClassWithText<IPackageReadMe>(nameof(IPackageReadMe.Text))}}
                    {{CreateModelClassWithText<IPackageLicense>(nameof(IPackageLicense.Text))}}
                    {{CreateModelClassWithText<IPackageReleaseNotes>(nameof(IPackageReleaseNotes.Text))}}
                {{TLW}}{{IF(NullableAnnotations)}}
                #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
                {{ENDIF}}
                {{TLW}}
                }
                """
        });
    }

    private FormattableString CreateGetPackageByIdMethod()
    {
        CancellationToken.ThrowIfCancellationRequested();

        MethodInfo? getPackageByIdMethod = typeof(IPackagesProvider).GetMethod(nameof(IPackagesProvider.GetPackageById));

        return $$"""
            {{TryGetDocumentation(getPackageByIdMethod)}}
            public static {{Create.Type<IPackage>()}}{{QM}} {{nameof(IPackagesProvider.GetPackageById)}}(string id)
            {
                switch (id)
                {
                    {{PropertyNamesByPackageId.Select(x => (FormattableString)$$"""
                        case "{{x.Key}}":
                            return {{x.Value}};
                        """)}}
                    default:
                        return null;
                }
            }
            {{Create.Type<IPackage>()}}{{QM}} {{Create.Type<IPackagesProvider>()}}.{{nameof(IPackagesProvider.GetPackageById)}}(string id)
            {
                return {{nameof(IPackagesProvider.GetPackageById)}}(id);
            }
            """;
    }

    private FormattableString CreateGetPackagesMethod()
    {
        CancellationToken.ThrowIfCancellationRequested();

        return $$"""
            {{TryGetDocumentation(typeof(IPackagesProvider), MemberTypes.Method, nameof(IPackagesProvider.GetPackages))}}
            public static {{Create.Type<IEnumerable<IPackage>>()}} {{nameof(IPackagesProvider.GetPackages)}}()
            {
                {{PropertyNamesByPackageId.Select(x => (FormattableString)$$"""
                yield return {{x.Value}};
                """)}}
                yield break;
            }
            {{Create.Type<IEnumerable<IPackage>>()}} {{Create.Type<IPackagesProvider>()}}.{{nameof(IPackagesProvider.GetPackages)}}()
            {
                return {{nameof(IPackagesProvider.GetPackages)}}();
            }
            """;
    }

    private FormattableString CreatePackageProperty(IPackageData package)
    {
        CancellationToken.ThrowIfCancellationRequested();

        string propertyName = PropertyNamesByPackageId[package.Id];
        string fieldName = CreateFieldName(propertyName);

        return $$"""
            private static {{Create.Type<IPackage>()}}{{QM}} {{fieldName}};
            private static {{Create.Type<IPackage>()}} {{propertyName}}
            {
                get
                {
                    if ({{fieldName}} == null)
                    {
                        {{fieldName}} = new {{GetFullModelClassExpression<IPackage>()}}()
                        {
                            {{nameof(IPackage.Id)}} = {{Create.String(package.Id)}},
                            {{nameof(IPackage.Version)}} = {{CreateVersion(package.Version)}},
                            {{nameof(IPackage.Description)}} = {{Create.String(package.Description)}},
                            {{nameof(IPackage.Owner)}} = {{Create.String(package.Owner)}},
                            {{nameof(IPackage.Product)}} = {{Create.String(package.Product)}},
                            {{nameof(IPackage.Title)}} = {{Create.String(package.Title)}},
                            {{nameof(IPackage.Authors)}} = {{Create.Strings(package.Authors)}},
                            {{nameof(IPackage.ProjectUrl)}} = {{Create.Uri(package.ProjectUrl)}},
                            {{nameof(IPackage.Copyright)}} = {{Create.String(package.Copyright)}},
                            {{nameof(IPackage.Tags)}} = {{Create.Strings(package.Tags)}},
                            {{nameof(IPackage.IsProjectDependency)}} = {{Create.Bool(package.IsProjectDependency)}},
                            {{nameof(IPackage.IsDevelopmentDependency)}} = {{Create.Bool(package.IsDevelopmentDependency)}},
                            {{nameof(IPackage.Repository)}} = {{CreatePackageRepository(package.Repository)}},
                            {{nameof(IPackage.ReleaseNotes)}} = {{CreatePackageReleaseNotes(package.Id, package.ReleaseNotes)}},
                            {{nameof(IPackage.Language)}} = {{Create.String(package.Language)}},
                            {{nameof(IPackage.Readme)}} = {{CreatePackageReadMe(package.Id, package.ReadMe)}},
                            {{nameof(IPackage.License)}} = {{CreatePackageLicense(package.Id, package.License)}},
                            {{nameof(IPackage.Icon)}} = {{CreatePackageIcon(package.Id, package.Icon)}},
                            {{nameof(IPackage.DependencyPackageIds)}} = {{Create.Strings(package.DependencyPackageIds)}},
                            {{nameof(IPackage.DependencyPackages)}} = {{CreateDependencyPackages(package.DependencyPackageIds)}},
                        };
                    }

                    return {{fieldName}};
                }
            }
            """;
    }

    private FormattableString CreateVersion(IPackageVersionData version)
    {
        CancellationToken.ThrowIfCancellationRequested();

        return $$"""
            new {{GetFullModelClassExpression<IPackageVersion>()}}()
            {
                {{nameof(IPackageVersion.Value)}} = {{Create.String(version.Value)}},
                {{nameof(IPackageVersion.IsPrerelease)}} = {{Create.Bool(version.IsPrerelease)}},
                {{nameof(IPackageVersion.Release)}} = {{Create.String(version.Release)}},
                {{nameof(IPackageVersion.ReleaseLabels)}} = {{Create.Strings(version.ReleaseLabels)}},
                {{nameof(IPackageVersion.HasMetadata)}} = {{Create.Bool(version.HasMetadata)}},
                {{nameof(IPackageVersion.Metadata)}} = {{Create.String(version.Metadata)}},
                {{nameof(IPackageVersion.Version)}} = {{Create.Version(version.Version)}},
                {{nameof(IPackageVersion.IsLegacyVersion)}} = {{Create.Bool(version.IsLegacyVersion)}},
                {{nameof(IPackageVersion.IsSemVer2)}} = {{Create.Bool(version.IsSemVer2)}},
            }
            """;
    }
    private FormattableString CreatePackageRepository(IPackageRepositoryData? repository)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (repository is null)
            return Create.Null;

        return $$"""
            new {{GetFullModelClassExpression<IPackageRepository>()}}()
            {
                {{nameof(IPackageRepository.Type)}} = {{Create.Enum(repository.Type)}},
                {{nameof(IPackageRepository.TypeName)}} = {{Create.String(repository.TypeName)}},
                {{nameof(IPackageRepository.Url)}} = {{Create.Uri(repository.Url)}},
                {{nameof(IPackageRepository.Branch)}} = {{Create.String(repository.Branch)}},
                {{nameof(IPackageRepository.Commit)}} = {{Create.String(repository.Commit)}},
            }
            """;
    }
    private FormattableString CreatePackageReleaseNotes(string packageId, IPackageReleaseNotesData? releaseNotes)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (releaseNotes is null)
            return Create.Null;

        IPackageContent<TextType>? content = ContentProvider.GetPackageReleaseNotes(packageId);

        return $$"""
            new {{GetFullModelClassExpression<IPackageReleaseNotes>()}}()
            {
                {{nameof(IPackageReleaseNotes.Url)}} = {{Create.Uri(releaseNotes.Url)}},
                {{nameof(IPackageReleaseNotes.Text)}} = {{CreateGetContent(packageId, content, nameof(IPackageData.ReleaseNotes))}},
                {{nameof(IPackageReleaseNotes.Type)}} = {{Create.Enum(content?.Type ?? default)}},
            }
            """;
    }
    private FormattableString CreatePackageReadMe(string packageId, IPackageReadMeData? readMe)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (readMe is null)
            return Create.Null;

        IPackageContent<TextType>? content = ContentProvider.GetPackageReadMe(packageId);

        return $$"""
            new {{GetFullModelClassExpression<IPackageReadMe>()}}()
            {
                {{nameof(IPackageReadMe.Text)}} = {{CreateGetContent(packageId, content, nameof(IPackageData.ReadMe))}},
                {{nameof(IPackageReadMe.Type)}} = {{Create.Enum(content?.Type ?? default)}},
            }
            """;
    }
    private FormattableString CreatePackageLicense(string packageId, IPackageLicenseData? license)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (license is null)
            return Create.Null;

        IPackageContent<TextType>? content = ContentProvider.GetPackageLicense(packageId);

        return $$"""
            new {{GetFullModelClassExpression<IPackageLicense>()}}()
            {
                {{nameof(IPackageLicense.Url)}} = {{Create.Uri(license.Url)}},
                {{nameof(IPackageLicense.Expression)}} = {{Create.String(license.Expression)}},
                {{nameof(IPackageLicense.Version)}} = {{Create.Version(license.Version)}},
                {{nameof(IPackageLicense.Text)}} = {{CreateGetContent(packageId, content, nameof(IPackageData.License))}},
                {{nameof(IPackageLicense.Type)}} = {{Create.Enum(content?.Type ?? default)}},
            }
            """;
    }
    private FormattableString CreatePackageIcon(string packageId, IPackageIconData? icon)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (icon is null)
            return Create.Null;

        IPackageContent<ImageType>? content = ContentProvider.GetPackageIcon(packageId);

        return $$"""
            new {{GetFullModelClassExpression<IPackageIcon>()}}()
            {
                {{nameof(IPackageIcon.Url)}} = {{Create.Uri(icon.Url)}},
                {{nameof(IPackageIcon.Type)}} = {{Create.Enum(content?.Type ?? default)}},
                {{nameof(IPackageIcon.HasContent)}} = {{Create.Bool(content is not null)}},
                {{nameof(IPackageIcon.OpenStream)}} = {{CreateGetContent(packageId, content, nameof(IPackageData.Icon))}},
            }
            """;
    }
    private FormattableString CreateDependencyPackages(IEnumerable<string> dependencyPackageIds)
    {
        CancellationToken.ThrowIfCancellationRequested();

        if (!dependencyPackageIds.Any())
            return $"""{Create.EmptyArray<IPackage>()}""";

        return $$"""
            new {{Create.Type<IPackage>()}}[]
            {
                {{dependencyPackageIds.Select(id => $"{PropertyNamesByPackageId[id]},")}}
            }
            """;
    }

    private FormattableString? CreateLoadMethods()
    {
        bool hasImageFiles = ContentProvider.HasImageFiles;
        bool hasTextFiles = ContentProvider.HasTextFiles;

        if (!hasImageFiles && !hasTextFiles)
            return null;

        switch (ContentWriteMode)
        {
            case ContentWriteMode.Embed:
                return $"""
                    {IF(hasImageFiles)}{CreateReadEmbeddedText()}{ENDIF}
                    {IF(hasImageFiles)}{CreateOpenEmbeddedImageStream()}{ENDIF}
                    """;

            case ContentWriteMode.File:
                return $"""
                    {IF(hasImageFiles)}{CreateReadFileText()}{ENDIF}
                    {IF(hasImageFiles)}{CreateOpenFileImageStream()}{ENDIF}
                    """;

            case ContentWriteMode.InCode:
                return $"""
                    {IF(hasImageFiles)}{CreateOpenCodeImageStream()}{ENDIF}
                    """;

            default:
                throw CreateContentWriteModeNotSupportedException();
        }

        FormattableString CreateReadEmbeddedText()
        {
            return $$"""
                private static string{{QM}} {{ContentMethods.ReadText}}(string fileName)
                {
                    {{Create.Type<Stream>()}}? stream = {{Create.Type<Assembly>()}}.GetExecutingAssembly().GetManifestResourceStream(fileName);

                    if (stream is null)
                        return null;

                    using ({{Create.Type<StreamReader>()}} reader = new {{Create.Type<StreamReader>()}}(stream))
                        return reader.ReadToEnd();
                }
                """;
        }

        FormattableString CreateOpenEmbeddedImageStream()
        {
            return $$"""
                private static {{Create.Type<Stream>()}} {{ContentMethods.OpenImageStream}}(string fileName)
                {
                    {{Create.Type<Stream>()}}{{QM}} stream = {{Create.Type<Assembly>()}}.GetExecutingAssembly().GetManifestResourceStream(fileName);

                    if (stream is null)
                        return {{Create.EmptyStream()}};

                    return stream;
                }
                """;
        }

        FormattableString CreateReadFileText()
        {
            return $$"""
                private static string{{QM}} {{ContentMethods.ReadText}}(string fileName)
                {
                    if ({{Create.Type(typeof(File))}}.Exists(fileName))
                        return {{Create.Type(typeof(File))}}.ReadAllText(fileName);

                    return null;
                }
                """;
        }

        FormattableString CreateOpenFileImageStream()
        {
            return $$"""
                private static {{Create.Type<Stream>()}} {{ContentMethods.OpenImageStream}}(string fileName)
                {
                    if ({{Create.Type(typeof(File))}}.Exists(fileName))
                        return {{Create.Type(typeof(File))}}.OpenRead(fileName);

                    return {{Create.EmptyStream()}};
                }
                """;
        }

        FormattableString CreateOpenCodeImageStream()
        {
            return $$"""
                private static {{Create.Type<Stream>()}} {{ContentMethods.OpenImageStream}}(byte[]{{QM}} bytes)
                {
                    if (bytes == null)
                        return {{Create.EmptyStream()}};

                    return new {{Create.Type<MemoryStream>()}}(bytes, writable: false);
                }
                """;
        }
    }

    private void CreateContentClasses(ICodegenContext codeContext)
    {
        foreach (IPackageData package in Packages)
        {
            string fileName = GetContentClassFileName(package.Id);

            FormattableString? code = CreateContentClass(package);

            if (code is not null)
                codeContext[fileName].Write(code);
        }
    }
    private FormattableString? CreateContentClass(IPackageData package)
    {
        bool hasContent = TryGetAllPackageContent(package,
            out IPackageContent<byte[], ImageType>? iconContent,
            out IPackageContent<string, TextType>? licenseContent,
            out IPackageContent<string, TextType>? releaseNotesContent,
            out IPackageContent<string, TextType>? readMeContent);

        if (!hasContent)
            return null;

        return CreateFile(new()
        {
            InNamespace = $$"""
                {{Access}} sealed partial class {{ClassName}}
                {
                    private sealed class {{GetContentCodeClassName(package.Id)}}
                    {
                        {{CreateImageContentProperty(iconContent, nameof(package.Icon))}}
                        {{CreateTextContentProperty(licenseContent, nameof(package.License))}}
                        {{CreateTextContentProperty(releaseNotesContent, nameof(package.ReleaseNotes))}}
                        {{CreateTextContentProperty(readMeContent, nameof(package.ReadMe))}}
                    }
                }
                """
        });
    }
    private bool TryGetAllPackageContent(IPackageData package,
        [MaybeNullWhen(false)] out IPackageContent<byte[], ImageType> iconContent,
        [MaybeNullWhen(false)] out IPackageContent<string, TextType> licenseContent,
        [MaybeNullWhen(false)] out IPackageContent<string, TextType> releaseNotesContent,
        [MaybeNullWhen(false)] out IPackageContent<string, TextType> readMeContent)
    {
        iconContent = ContentProvider.GetPackageIcon(package.Id);
        licenseContent = ContentProvider.GetPackageLicense(package.Id);
        releaseNotesContent = ContentProvider.GetPackageReleaseNotes(package.Id);
        readMeContent = ContentProvider.GetPackageReadMe(package.Id);

        return iconContent is not null
            || licenseContent is not null
            || releaseNotesContent is not null
            || readMeContent is not null;
    }

    private FormattableString? CreateImageContentProperty(IPackageContent<byte[], ImageType>? content, string propertyName)
    {
        return CreateContentProperty(content, propertyName, FileToCode);

        FormattableString FileToCode(IPackageContent<byte[], ImageType> file)
            => Create.ByteArray(file.LoadContent());
    }
    private FormattableString? CreateTextContentProperty(IPackageContent<string, TextType>? content, string propertyName)
    {
        return CreateContentProperty(content, propertyName, FileToCode);

        FormattableString FileToCode(IPackageContent<string, TextType> file)
            => Create.String(file.LoadContent());
    }
    private FormattableString? CreateContentProperty<TContent, TType>(IPackageContent<TContent, TType>? content, string propertyName, Func<IPackageContent<TContent, TType>, FormattableString> toCode)
        where TContent : class
        where TType : struct, Enum
    {
        if (content is null)
            return null;

        string fieldName = CreateFieldName(propertyName);

        return $$"""
            private static {{Create.Type<TContent>()}}{{QM}} {{fieldName}};
            public static {{Create.Type<TContent>()}}{{QM}} {{propertyName}}
            {
                get
                {
                    if ({{fieldName}} == null)
                    {
                        {{fieldName}} = {{toCode(content)}};
                    }
            
                    return {{fieldName}};
                }
            }
            """;
    }

    private FormattableString CreateModelClassWithText<TInterface>(string textPropertyName)
    {
        FormattableString additionalMembers = $$"""
            public {{Create.Type<Func<string?>>()}}{{QM}} {{textPropertyName}} { get; set; }
            
            private string{{QM}} _text;
            string{{QM}} {{Create.Type<TInterface>()}}.{{textPropertyName}}
            {
                get
                {
                    if (_text != null)
                        return null;

                    if ({{textPropertyName}} == null)
                        return null;

                    string{{QM}} text = {{textPropertyName}}();
            
                    _text = text;

                    return text;
                }
            }
            """;

        return CreateModelClass<TInterface>(additionalMembers, textPropertyName);
    }
    private FormattableString CreateModelClassWithImage<TInterface>(string openStreamMethodName)
    {
        FormattableString additionalMembers = $$"""
            public {{Create.Type<Func<Stream?>>()}}{{QM}} {{openStreamMethodName}} { get; set; }
            
            {{Create.Type<Stream>()}} {{Create.Type<TInterface>()}}.{{openStreamMethodName}}()
            {
                if ({{openStreamMethodName}} == null)
                    return {{Create.EmptyStream()}};

                {{Create.Type<Stream>()}}{{QM}} stream = {{openStreamMethodName}}();
            
                if (stream == null)
                    return {{Create.EmptyStream()}};
            
                return stream;
            }
            """;

        return CreateModelClass<TInterface>(additionalMembers);
    }
    private FormattableString CreateModelClass<TInterface>(FormattableString? additionalMembers = null, string? ignorePropertyName = null)
    {
        CancellationToken.ThrowIfCancellationRequested();

        Type type = typeof(TInterface);

        return $$"""
            private sealed class {{type.Name.Substring(1)}} : {{Create.Type(type)}}
            {
                {{type.GetProperties().Where(p => p.Name != ignorePropertyName).Select(CreatePropertyString)}}
                {{IF(additionalMembers is not null)}}
                {{TLW}}
                {{additionalMembers}}
                {{ENDIF}}
                {{TLW}}
            }
            """;

        FormattableString CreatePropertyString(PropertyInfo p)
            => $$"""public {{Create.Type(p.PropertyType)}}{{(p.IsNullable() ? QM : null)}} {{p.Name}} { get; set; }""";
    }

    private FormattableString GetFullModelClassExpression<T>()
        => $"{ClassName}.{typeof(T).Name.Substring(1)}";

    private FormattableString CreateGetContent(string packageId, IPackageContent<ImageType>? file, string contentType)
    {
        if (file is null)
            return Create.Null;

        FormattableString parameter = ContentWriteMode == ContentWriteMode.InCode
            ? $"{GetContentCodeClassName(packageId)}.{contentType}"
            : Create.String(file.File.Name);

        return $"() => {ContentMethods.OpenImageStream}({parameter})";
    }
    private FormattableString CreateGetContent(string packageId, IPackageContent<TextType>? content, string contentType)
    {
        if (content is null)
            return Create.Null;

        if (ContentWriteMode == ContentWriteMode.InCode)
            return $"() => {GetContentCodeClassName(packageId)}.{contentType}";

        return $"() => {ContentMethods.ReadText}({Create.String(content.File.Name)})";
    }

    private string CreateFieldName(string name)
    {
        return $"_{char.ToLower(name[0])}{name.Substring(1)}";
    }
    private string GetContentCodeClassName(string packageId)
    {
        return $"PackageContent_{PropertyNamesByPackageId[packageId]}";
    }
    private string GetContentClassFileName(string packageId)
        => $"{ClassName}.{PropertyNamesByPackageId[packageId]}.g.cs";
}
