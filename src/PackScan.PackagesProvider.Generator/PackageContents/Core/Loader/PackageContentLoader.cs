using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Microsoft.VisualStudio.Threading;

using PackScan.PackagesProvider.Generator.Files;
using PackScan.PackagesProvider.Generator.Utils;
using PackScan.PackagesReader.Abstractions;

using Path = System.IO.Path;
using Stream = System.IO.Stream;

namespace PackScan.PackagesProvider.Generator.PackageContents.Core.Loader;

internal abstract class PackageContentLoader<TContent, TType> : IPackageContentLoader<TContent, TType>
    where TContent : class
    where TType : struct, Enum
{
    private readonly Dictionary<string, AsyncLazy<IPackageContent<TContent, TType>?>> _runningDownloads = new(StringComparer.OrdinalIgnoreCase);
    private readonly IPackagesProviderFilesManager _filesManager;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _downloadCacheFolder;

    protected abstract IReadOnlyDictionary<string, TType> MimeTypeTypeMapping { get; }
    protected abstract IReadOnlyDictionary<string, TType> FileExtensionTypeMapping { get; }

    protected PackageContentLoader(IPackagesProviderFilesManager filesManager, IHttpClientFactory httpClientFactory, string downloadCacheFolder)
    {
        _filesManager = filesManager;
        _httpClientFactory = httpClientFactory;
        _downloadCacheFolder = downloadCacheFolder;
    }

    public IPackageContent<TContent, TType>? TryLoad(ContentLoadMode loadMode, IPackageContentData? contentData, CancellationToken cancellationToken)
    {
        if (contentData is null)
            return null;

        switch (loadMode)
        {
            case ContentLoadMode.OnlyFile:
                return TryReadFile(contentData.FilePath, cancellationToken);

            case ContentLoadMode.OnlyUrl:
                return TryDownload(contentData.Url, cancellationToken);

            case ContentLoadMode.PreferFile:
                return TryReadFile(contentData.FilePath, cancellationToken)
                    ?? TryDownload(contentData.Url, cancellationToken);

            case ContentLoadMode.PreferUrl:
                return TryDownload(contentData.Url, cancellationToken)
                    ?? TryReadFile(contentData.FilePath, cancellationToken);

            case ContentLoadMode.FileIfHasNoUrl:
                return !HasUrl(contentData.Url)
                    ? TryReadFile(contentData.FilePath, cancellationToken)
                    : null;

            case ContentLoadMode.UrlIfHasNoFile:
                return !HasFile(contentData.FilePath)
                    ? TryDownload(contentData.Url, cancellationToken)
                    : null;

            default:
            case ContentLoadMode.None:
                return null;
        }
    }

    private IPackageContent<TContent, TType>? TryReadFile(string? filePath, CancellationToken cancellationToken)
    {
        if (!HasFile(filePath))
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        TType type = GetContentType(filePath);

        return AddFileAndCreateContent(filePath, type);
    }
    private static bool HasFile([NotNullWhen(true)] string? filePath)
    {
        return filePath?.Length > 0
            && File.Exists(filePath);
    }

    [SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Since the rest of the project is synchronous, so is this method. Unfortunately there is no synchronous download implementation.")]
    private IPackageContent<TContent, TType>? TryDownload(Uri? url, CancellationToken cancellationToken)
    {
        if (!HasUrl(url))
            return null;

        SynchronizationContext syncContext = SynchronizationContext.Current;

        SynchronizationContext.SetSynchronizationContext(null);

        try
        {
            return TryDownloadWithCacheAsync(url, cancellationToken).Result;
        }
        catch (AggregateException ex) when (ex.InnerExceptions.Count == 1)
        {
            throw ex.InnerExceptions[0];
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(syncContext);
        }
    }
    private static bool HasUrl([NotNullWhen(true)] Uri? url)
    {
        return url is not null;
    }
    [SuppressMessage("Usage", "VSTHRD012:Provide JoinableTaskFactory where allowed", Justification = "AsyncLazy is only needed as a thread-safe cache and since the rest of the project is synchronous, it is not used here")]
    private async Task<IPackageContent<TContent, TType>?> TryDownloadWithCacheAsync(Uri url, CancellationToken cancellationToken)
    {
        AsyncLazy<IPackageContent<TContent, TType>?> lazy;
        bool isNew;

        lock (_runningDownloads)
        {
            if (isNew = !_runningDownloads.TryGetValue(url.AbsoluteUri, out lazy))
            {
                lazy = new(() => TryDownloadAsync(url, cancellationToken));

                _runningDownloads.Add(url.AbsoluteUri, lazy);
            }
        }

        IPackageContent<TContent, TType>? content = await lazy.GetValueAsync(cancellationToken);

        if (isNew)
        {
            lock (_runningDownloads)
                _runningDownloads.Remove(url.AbsoluteUri);
        }

        return content;
    }
    private async Task<IPackageContent<TContent, TType>?> TryDownloadAsync(Uri url, CancellationToken cancellationToken)
    {
        string urlMD5Hash = CalcMD5Hash(url);
        string tempFilePath = Path.Combine(_downloadCacheFolder, urlMD5Hash);

        if (!Directory.Exists(_downloadCacheFolder))
            Directory.CreateDirectory(_downloadCacheFolder);

        lock (urlMD5Hash)
        {
            if (File.Exists(tempFilePath))
            {
                cancellationToken.ThrowIfCancellationRequested();

                TType type = GetContentType(tempFilePath);

                return AddFileAndCreateContent(tempFilePath, type);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();

        return await TryDownloadAsync(url, tempFilePath, cancellationToken);
    }
    private static string CalcMD5Hash(Uri uri)
    {
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(uri.AbsoluteUri);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return BitConverter
                .ToString(hashBytes)
                .Replace("-", string.Empty)
                .ToLower();
        }
    }
    private async Task<IPackageContent<TContent, TType>?> TryDownloadAsync(Uri url, string tempFilePath, CancellationToken cancellationToken)
    {
        using HttpRequestMessage message = new(HttpMethod.Get, url);

        foreach (string mimeType in MimeTypeTypeMapping.Keys)
            message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(mimeType));

        cancellationToken.ThrowIfCancellationRequested();

        using HttpClient httpClient = _httpClientFactory.CreateClient();
        using HttpResponseMessage response = await httpClient.SendAsync(message, cancellationToken);

        if (!response.IsSuccessStatusCode)
            return null;

        cancellationToken.ThrowIfCancellationRequested();

        using (Stream tempStream = File.Create(tempFilePath))
            await response.Content.CopyToAsync(tempStream);

        TType type = GetContentType(response, tempFilePath);

        return AddFileAndCreateContent(tempFilePath, type);
    }

    private IPackageContent<TContent, TType> AddFileAndCreateContent(string filePath, TType type)
    {
        IPackagesProviderFile file = _filesManager.AddPhysical(filePath);

        return CreateContent(file, type);
    }
    protected abstract PackageContent<TContent, TType> CreateContent(IPackagesProviderFile file, TType type);

    protected virtual TType GetContentType(HttpResponseMessage response, string filePath)
    {
        response.Headers.TryGetValues("Content-Type", out IEnumerable<string>? contentTypes);

        if (contentTypes?.Any() != true)
            return default;

        foreach ((string mimeType, TType type) in MimeTypeTypeMapping)
        {
            if (contentTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase))
                return type;
        }

        return default;
    }
    protected virtual TType GetContentType(string filePath)
    {
        string fileExtension = Path.GetExtension(filePath);

        if (FileExtensionTypeMapping.TryGetValue(fileExtension, out TType type))
            return type;

        return default;
    }
}
