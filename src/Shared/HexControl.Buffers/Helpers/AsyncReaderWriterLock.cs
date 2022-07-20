using JetBrains.Annotations;

namespace HexControl.Buffers.Helpers;

[PublicAPI]
internal sealed class AsyncReaderWriterLock : IDisposable
{
    private readonly SemaphoreSlim _readSemaphore = new(1, 1);
    private readonly SemaphoreSlim _writeSemaphore = new(1, 1);
    private int _readerCount;

    public async ValueTask<LockScope> AcquireWriterLockAsync(CancellationToken cancellationToken = default)
    {
        await _writeSemaphore.WaitAsync(cancellationToken);
        try
        {
            await _readSemaphore.WaitAsync(cancellationToken);
            return new LockScope(this, false);
        }
        catch
        {
            _writeSemaphore.Release();
            throw;
        }
    }

    public LockScope AcquireWriterLock(CancellationToken cancellationToken = default)
    {
        _writeSemaphore.Wait(cancellationToken);
        try
        {
            _readSemaphore.Wait(cancellationToken);
            return new LockScope(this, false);
        }
        catch
        {
            _writeSemaphore.Release();
            throw;
        }
    }

    public void ReleaseWriterLock()
    {
        _readSemaphore.Release();
        _writeSemaphore.Release();
    }

    public async ValueTask<LockScope> AcquireReaderLockAsync(CancellationToken cancellationToken = default)
    {
        await _writeSemaphore.WaitAsync(cancellationToken);

        if (Interlocked.Increment(ref _readerCount) is 1)
        {
            try
            {
                await _readSemaphore.WaitAsync(cancellationToken);
            }
            catch
            {
                Interlocked.Decrement(ref _readerCount);
                _writeSemaphore.Release();
                throw;
            }
        }

        _writeSemaphore.Release();

        return new LockScope(this, true);
    }

    public LockScope AcquireReaderLock(CancellationToken cancellationToken = default)
    {
        _writeSemaphore.Wait(cancellationToken);

        if (Interlocked.Increment(ref _readerCount) is 1)
        {
            try
            {
                _readSemaphore.Wait(cancellationToken);
            }
            catch
            {
                Interlocked.Decrement(ref _readerCount);
                _writeSemaphore.Release();
                throw;
            }
        }

        _writeSemaphore.Release();

        return new LockScope(this, true);
    }

    public void ReleaseReaderLock()
    {
        if (Interlocked.Decrement(ref _readerCount) is 0)
        {
            _readSemaphore.Release();
        }
    }

    public void Dispose()
    {
        _writeSemaphore.Dispose();
        _readSemaphore.Dispose();
    }
    
    internal readonly struct LockScope : IDisposable
    {
        private readonly AsyncReaderWriterLock _lock;
        private readonly bool _reader;

        public LockScope(AsyncReaderWriterLock @lock, bool reader)
        {
            _lock = @lock;
            _reader = reader;
        }

        public void Dispose()
        {
            if (_reader)
            {
                _lock.ReleaseReaderLock();
            }
            else
            {
                _lock.ReleaseWriterLock();
            }
        }
    }
}