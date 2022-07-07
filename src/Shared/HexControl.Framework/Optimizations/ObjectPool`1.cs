namespace HexControl.Framework.Optimizations;

internal sealed class ObjectPool<TObject> : IDisposable
    where TObject : new()
{
    private static readonly Lazy<ObjectPool<TObject>> Lazy = new(() => new ObjectPool<TObject>(20));

    private readonly Stack<TObject> _entries;

    private readonly object _lock;
    private readonly int _maxEntries;
    private int _count;

    public ObjectPool(int maxEntries)
    {
        _maxEntries = maxEntries;
        _entries = new Stack<TObject>(maxEntries);
        _lock = new object();
    }

    public static ObjectPool<TObject> Shared => Lazy.Value;

    public void Dispose()
    {
        lock (_lock)
        {
            _count = 0;
            while (_entries.Count > 0)
            {
                var entry = _entries.Pop();
                using var item = entry as IDisposable;
            }
        }
    }

    public TObject Rent()
    {
        lock (_lock)
        {
            if (_entries.Count > 0)
            {
                return _entries.Pop();
            }
        }

        _count++;
        return new TObject();
    }

    public void Return(TObject item)
    {
        lock (_lock)
        {
            if (_count < _maxEntries)
            {
                _entries.Push(item);
            }
            else
            {
                using (item as IDisposable)
                {
                    _count--;
                }
            }
        }
    }
}