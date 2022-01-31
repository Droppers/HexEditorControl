namespace HexControl.Core.Helpers;

internal class ObjectCache<TKey, TValue> : IDisposable
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _entries;
    private readonly Func<TKey, TValue> _factory;
    private readonly object _lock;

    public ObjectCache(Func<TKey, TValue> factory)
    {
        _factory = factory;
        _lock = new object();
        _entries = new Dictionary<TKey, TValue>();
    }

    public TValue? this[TKey? key] => Get(key);

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var (_, entry) in _entries)
            {
                using var _ = entry as IDisposable;
            }

            _entries.Clear();
        }
    }

    private TValue? Get(TKey? key)
    {
        if (key is null)
        {
            return default;
        }

        lock (_lock)
        {
            if (_entries.TryGetValue(key, out var value))
            {
                return value;
            }

            var entry = _factory(key);
            _entries[key] = entry;
            return entry;
        }
    }
}