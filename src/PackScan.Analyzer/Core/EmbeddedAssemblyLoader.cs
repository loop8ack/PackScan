using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace PackScan.Analyzer.Core;

/// <summary>
/// Workaround to embed referenced DLLs so that they can be loaded at runtime.
/// All dependencies are added as EmbeddedResource (see Target: EmbedReferencedAssemblies) and then loaded in this class.
/// </summary>
internal static class EmbeddedAssemblyLoader
{
    private static readonly Assembly _assembly = typeof(EmbeddedAssemblyLoader).Assembly;
    private static readonly ConcurrentDictionary<string, Assembly?> _assemblyCache = new();
    private static int _initializedFlag = 0;

    public static void Init()
    {
        if (Interlocked.Exchange(ref _initializedFlag, 1) == 0)
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    private static Assembly? OnAssemblyResolve(object sender, ResolveEventArgs e)
        => _assemblyCache.GetOrAdd(e.Name, TryLoadEmbeddedAssembly);

    [SuppressMessage("MicrosoftCodeAnalysisCorrectness", "RS1035:Do not use APIs banned for analyzers")]
    private static Assembly? TryLoadEmbeddedAssembly(string assemblyName)
    {
        string embeddedName = new AssemblyName(assemblyName).Name + ".dll";
        Stream? stream = _assembly.GetManifestResourceStream(embeddedName);

        if (stream is null)
            return null;

        MemoryStream memory = new MemoryStream((int)stream.Length);

        stream.CopyTo(memory);

        return Assembly.Load(memory.ToArray());
    }
}
