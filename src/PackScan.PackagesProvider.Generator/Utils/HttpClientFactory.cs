namespace PackScan.PackagesProvider.Generator.Utils;

internal sealed class HttpClientFactory : IHttpClientFactory, IDisposable
{
    private Lazy<HttpClientHandler>? _lazyHandler = new(LazyThreadSafetyMode.ExecutionAndPublication);

    public HttpClient CreateClient(string name)
    {
        HttpClientHandler handler = _lazyHandler?.Value
            ?? throw new ObjectDisposedException(nameof(HttpClientFactory));

        return new HttpClient(handler, disposeHandler: false);
    }

    public void Dispose()
    {
        Lazy<HttpClientHandler>? lazyHandler = Interlocked.Exchange(ref _lazyHandler, null);

        if (lazyHandler?.IsValueCreated == true)
            lazyHandler.Value.Dispose();
    }
}
