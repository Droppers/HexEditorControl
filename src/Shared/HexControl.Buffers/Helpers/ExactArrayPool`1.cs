namespace HexControl.Buffers.Helpers;

internal sealed class ExactArrayPool<TType>
{
    private static readonly Lazy<ExactArrayPool<TType>> Lazy = new(() => new ExactArrayPool<TType>(30));
    private readonly Dictionary<int, Stack<TType[]>> _entries;

    private readonly object _lock;
    private readonly int _maxSize;

    public ExactArrayPool(int maxSize)
    {
        _maxSize = maxSize;
        _entries = new Dictionary<int, Stack<TType[]>>();
        _lock = new object();
    }

    public static ExactArrayPool<TType> Shared => Lazy.Value;

    public void Return(TType[] array)
    {
        lock (_lock)
        {
            if (!_entries.TryGetValue(array.Length, out var pool))
            {
                throw new InvalidOperationException($"Tried to return array to pool with size {array.Length}.");
            }

            pool.Push(array);
        }
    }

    public TType[] Rent(int size)
    {
        if (size > _maxSize)
        {
            return new TType[size];
        }

        lock (_lock)
        {
            if (!_entries.TryGetValue(size, out var pool))
            {
                pool = _entries[size] = new Stack<TType[]>();
            }

            return pool.Count > 0 ? pool.Pop() : new TType[size];
        }
    }
}