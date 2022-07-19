namespace HexControl.Buffers.Helpers;

internal static class SemaphoreSlimExtensions
{
    public static async ValueTask<SemaphoreLock> LockAsync(this SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        return new SemaphoreLock(semaphore);
    }

    public static SemaphoreLock Lock(this SemaphoreSlim semaphore)
    {
        semaphore.Wait();
        return new SemaphoreLock(semaphore);
    }
}