namespace HexControl.Buffers.Helpers;

internal readonly struct SemaphoreLock : IDisposable
{
    private readonly SemaphoreSlim _semaphore;

    public SemaphoreLock(SemaphoreSlim semaphore)
    {
        _semaphore = semaphore;
    }

    public void Dispose()
    {
        _semaphore.Release();
    }
}