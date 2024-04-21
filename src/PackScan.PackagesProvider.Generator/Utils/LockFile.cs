namespace PackScan.PackagesProvider.Generator.Utils;

internal static class LockFile
{
    public static Task<IDisposable> LockAsync(string filePath, TimeSpan timeout, TimeSpan retryDelay, CancellationToken cancellationToken)
    {
        if (timeout > Timeout.InfiniteTimeSpan)
        {
            using CancellationTokenSource timeoutCts = new(timeout);
            using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellationToken);

            return CoreLockAsync(filePath, retryDelay, timeoutCts.Token, cts.Token);
        }

        return CoreLockAsync(filePath, retryDelay, default, cancellationToken);
    }

    private static async Task<IDisposable> CoreLockAsync(string filePath, TimeSpan retryDelay, CancellationToken timeoutCancellationToken, CancellationToken cancellationToken)
    {
        string lockFilePath = filePath + ".lock";

        FileStream lockFileStream;
        int retriesCount = 0;

        try
        {
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                retriesCount++;

                try
                {
                    lockFileStream = new(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);

                    break;
                }
                catch (IOException)
                {
                    await Task.Delay(retryDelay, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException ex)
            when (timeoutCancellationToken.IsCancellationRequested)
        {
            throw new IOException("Could not acquire file lock access after " + retriesCount + " attempts.", ex);
        }

        return new Handle(lockFileStream, lockFilePath);
    }

    public readonly struct Handle : IDisposable
    {
        private readonly FileStream _fileStream;
        private readonly string _filePath;

        public Handle(FileStream fileStream, string filePath)
        {
            _fileStream = fileStream;
            _filePath = filePath;
        }

        public void Dispose()
        {
            _fileStream.Dispose();
            File.Delete(_filePath);
        }
    }
}
