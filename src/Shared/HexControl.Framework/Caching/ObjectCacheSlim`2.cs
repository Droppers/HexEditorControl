using HexControl.Framework.Collections;

#pragma warning disable CS8634

namespace HexControl.Framework.Caching;

internal class ObjectCacheSlim<TKey, TValue> : IDisposable
    where TKey : IEquatable<TKey>
    where TValue : class?
{
    private readonly DictionarySlim<TKey, TValue> _entries;
    private readonly Func<TKey, TValue> _factory;
    private readonly object _lock;

    public ObjectCacheSlim(Func<TKey, TValue> factory)
    {
        _factory = factory;
        _lock = new object();
        _entries = new DictionarySlim<TKey, TValue>();
    }

    public TValue? this[TKey? key] => Get(key);

    public void Dispose()
    {
        lock (_lock)
        {
            var enumerator = _entries.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var entry = enumerator.Value;
                if (entry is IDisposable disposable)
                {
                    disposable.Dispose();
                }
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
            ref var entryRef = ref _entries.GetOrAddValueRef(key);
            entryRef = entry;
            return entry;
        }
    }
}