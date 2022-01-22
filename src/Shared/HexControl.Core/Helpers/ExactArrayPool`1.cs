using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.Core.Helpers
{
    internal sealed class ExactArrayPool<TType>
    {
        private static readonly Lazy<ExactArrayPool<TType>> Lazy = new(() => new ExactArrayPool<TType>(30));

        public static ExactArrayPool<TType> Instance => Lazy.Value;

        private readonly object _lock;
        private readonly int _maxSize;
        private readonly Dictionary<int, Queue<TType[]>> _entries;

        public ExactArrayPool(int maxSize)
        {
            _maxSize = maxSize;
            _entries = new Dictionary<int, Queue<TType[]>>();
            _lock = new object();
        }

        public void Return(TType[] array)
        {
            lock (_lock)
            {
                if (!_entries.TryGetValue(array.Length, out var pool))
                {
                    throw new InvalidOperationException($"Tried to return array to pool with size {array.Length}.");
                }

                pool.Enqueue(array);
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
                    pool = _entries[size] = new Queue<TType[]>();
                }

                return pool.Count > 0 ? pool.Dequeue() : new TType[size];
            }
        }
    }
}
