using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.Core.Helpers
{
    internal sealed class ObjectPool<TObject> : IDisposable
        where TObject : new()
    {
        private readonly object _lock;
        private readonly Queue<TObject> _queue;
        private readonly int _size;
        private int _count;

        private static readonly Lazy<ObjectPool<TObject>> Lazy = new(() => new ObjectPool<TObject>(20));
        public static ObjectPool<TObject> Shared => Lazy.Value;

        public ObjectPool(int size)
        {
            _size = size;
            _lock = new object();
            _queue = new Queue<TObject>();
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _count = 0;
                while (_queue.Count > 0)
                {
                    using var item = _queue.Dequeue() as IDisposable;
                }
            }
        }

        public TObject Rent()
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    return _queue.Dequeue();
                }

                _count++;
                return new TObject();
            }
        }

        public void Return(TObject item)
        {
            lock (_lock)
            {
                if (_count < _size)
                {
                    _queue.Enqueue(item);
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
}
