// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable SA1512

// The DictionarySlim<TKey, TValue> type is originally from CoreFX labs, see
// the source repository on GitHub at https://github.com/dotnet/corefxlab.

using System.Collections;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace HexControl.Framework.Collections;

/// <summary>
/// A lightweight Dictionary with three principal differences compared to <see cref="Dictionary{TKey, TValue}"/>
///
/// 1) It is possible to do "get or add" in a single lookup. For value types, this also saves a copy of the value.
/// 2) It assumes it is cheap to equate values.
/// 3) It assumes the keys implement <see cref="IEquatable{TKey}"/> and they are cheap and sufficient.
/// </summary>
/// <typeparam name="TKey">The type of keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
/// <remarks>
/// 1) This avoids having to do separate lookups (<see cref="Dictionary{TKey, TValue}.TryGetValue(TKey, out TValue)"/>
/// followed by <see cref="Dictionary{TKey, TValue}.Add(TKey, TValue)"/>.
/// There is not currently an API exposed to get a value by ref without adding if the key is not present.
/// 2) This means it can save space by not storing hash codes.
/// 3) This means it can avoid storing a comparer, and avoid the likely virtual call to a comparer.
/// </remarks>
[PublicAPI]
[DebuggerDisplay("Count = {Count}")]
internal class DictionarySlim<TKey, TValue> : IDictionarySlim<TKey, TValue>
    where TKey : IEquatable<TKey>
    where TValue : class
{
    // ReSharper disable once StaticMemberInGenericType
    private static readonly int[] SizeOneIntArray = new int[1];

    /// <summary>
    /// A reusable array of <see cref="Entry"/> items with a single value.
    /// This is used when a new <see cref="DictionarySlim{TKey,TValue}"/> instance is
    /// created, or when one is cleared. The first item being added to this collection
    /// will immediately cause the first resize (see <see cref="AddKey"/> for more info).
    /// </summary>
    private static readonly Entry[] InitialEntries = new Entry[1];

    /// <summary>
    /// The current number of items stored in the map.
    /// </summary>
    private int _count;

    /// <summary>
    /// The 1-based index for the start of the free list within <see cref="_entries"/>.
    /// </summary>
    private int _freeList = -1;

    /// <summary>
    /// The array of 1-based indices for the <see cref="Entry"/> items stored in <see cref="_entries"/>.
    /// </summary>
    private int[] _buckets;

    /// <summary>
    /// The array of currently stored key-value pairs (ie. the lists for each hash group).
    /// </summary>
    private Entry[] _entries;

    /// <summary>
    /// A type representing a map entry, ie. a node in a given list.
    /// </summary>
    private struct Entry
    {
        /// <summary>
        /// The key for the value in the current node.
        /// </summary>
        public TKey key;

        /// <summary>
        /// The value in the current node, if present.
        /// </summary>
        public TValue? value;

        /// <summary>
        /// The 0-based index for the next node in the current list.
        /// </summary>
        public int next;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionarySlim{TKey, TValue}"/> class.
    /// </summary>
    public DictionarySlim()
    {
        _buckets = SizeOneIntArray;
        _entries = InitialEntries;
    }

    /// <inheritdoc/>
    public int Count => _count;

    /// <inheritdoc/>
    public TValue this[TKey key]
    {
        get
        {
            var entries = _entries;

            for (var i = _buckets[key.GetHashCode() & (_buckets.Length - 1)] - 1;
                 (uint)i < (uint)entries.Length;
                 i = entries[i].next)
            {
                if (key.Equals(entries[i].key))
                {
                    return entries[i].value!;
                }
            }

            ThrowArgumentExceptionForKeyNotFound(key);

            return default!;
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _count = 0;
        _freeList = -1;
        _buckets = SizeOneIntArray;
        _entries = InitialEntries;
    }

    /// <summary>
    /// Checks whether or not the dictionary contains a pair with a specified key.
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <returns>Whether or not the key was present in the dictionary.</returns>
    public bool ContainsKey(TKey key)
    {
        var entries = _entries;

        for (var i = _buckets[key.GetHashCode() & (_buckets.Length - 1)] - 1;
             (uint)i < (uint)entries.Length;
             i = entries[i].next)
        {
            if (key.Equals(entries[i].key))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the value if present for the specified key.
    /// </summary>
    /// <param name="key">The key to look for.</param>
    /// <param name="value">The value found, otherwise <see langword="default"/>.</param>
    /// <returns>Whether or not the key was present.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        var entries = _entries;

        for (var i = _buckets[key.GetHashCode() & (_buckets.Length - 1)] - 1;
             (uint)i < (uint)entries.Length;
             i = entries[i].next)
        {
            if (key.Equals(entries[i].key))
            {
                value = entries[i].value!;

                return true;
            }
        }

        value = default!;

        return false;
    }

    /// <inheritdoc/>
    public bool TryRemove(TKey key)
    {
        return TryRemove(key, out _);
    }

    /// <summary>
    /// Tries to remove a value with a specified key, if present.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <param name="result">The removed value, if it was present.</param>
    /// <returns>Whether or not the key was present.</returns>
    public bool TryRemove(TKey key, out TValue? result)
    {
        var entries = _entries;
        var bucketIndex = key.GetHashCode() & (_buckets.Length - 1);
        var entryIndex = _buckets[bucketIndex] - 1;
        var lastIndex = -1;

        while (entryIndex != -1)
        {
            var candidate = entries[entryIndex];

            if (candidate.key.Equals(key))
            {
                if (lastIndex != -1)
                {
                    entries[lastIndex].next = candidate.next;
                }
                else
                {
                    _buckets[bucketIndex] = candidate.next + 1;
                }

                entries[entryIndex] = default;
                entries[entryIndex].next = -3 - _freeList;

                _freeList = entryIndex;
                _count--;

                result = candidate.value;

                return true;
            }

            lastIndex = entryIndex;
            entryIndex = candidate.next;
        }

        result = null;

        return false;
    }

    /// <summary>
    /// Gets the value for the specified key, or, if the key is not present,
    /// adds an entry and returns the value by ref. This makes it possible to
    /// add or update a value in a single look up operation.
    /// </summary>
    /// <param name="key">Key to look for</param>
    /// <returns>Reference to the new or existing value</returns>
    public ref TValue? GetOrAddValueRef(TKey key)
    {
        var entries = _entries;
        var bucketIndex = key.GetHashCode() & (_buckets.Length - 1);

        for (var i = _buckets[bucketIndex] - 1;
             (uint)i < (uint)entries.Length;
             i = entries[i].next)
        {
            if (key.Equals(entries[i].key))
            {
                return ref entries[i].value;
            }
        }

        return ref AddKey(key, bucketIndex);
    }

    /// <summary>
    /// Creates a slot for a new value to add for a specified key.
    /// </summary>
    /// <param name="key">The key to use to add the new value.</param>
    /// <param name="bucketIndex">The target bucked index to use.</param>
    /// <returns>A reference to the slot for the new value to add.</returns>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private ref TValue? AddKey(TKey key, int bucketIndex)
    {
        var entries = _entries;
        int entryIndex;

        if (_freeList != -1)
        {
            entryIndex = _freeList;

            _freeList = -3 - entries[_freeList].next;
        }
        else
        {
            if (_count == entries.Length || entries.Length == 1)
            {
                entries = Resize();
                bucketIndex = key.GetHashCode() & (_buckets.Length - 1);
            }

            entryIndex = _count;
        }

        entries[entryIndex].key = key;
        entries[entryIndex].next = _buckets[bucketIndex] - 1;

        _buckets[bucketIndex] = entryIndex + 1;
        _count++;

        return ref entries[entryIndex].value;
    }

    /// <summary>
    /// Resizes the current dictionary to reduce the number of collisions
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    private Entry[] Resize()
    {
        var count = _count;
        var newSize = _entries.Length * 2;

        if ((uint)newSize > int.MaxValue)
        {
            ThrowInvalidOperationExceptionForMaxCapacityExceeded();
        }

        var entries = new Entry[newSize];

        Array.Copy(_entries, 0, entries, 0, count);

        var newBuckets = new int[entries.Length];

        while (count-- > 0)
        {
            var bucketIndex = entries[count].key.GetHashCode() & (newBuckets.Length - 1);

            entries[count].next = newBuckets[bucketIndex] - 1;

            newBuckets[bucketIndex] = count + 1;
        }

        _buckets = newBuckets;
        _entries = entries;

        return entries;
    }

    /// <inheritdoc cref="IEnumerable{T}.GetEnumerator"/>
    [System.Diagnostics.Contracts.Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Enumerator GetEnumerator() => new(this);

    /// <summary>
    /// Enumerator for <see cref="DictionarySlim{TKey,TValue}"/>.
    /// </summary>
    public ref struct Enumerator
    {
        private readonly Entry[] _entries;
        private int _index;
        private int _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Enumerator(DictionarySlim<TKey, TValue> dictionary)
        {
            _entries = dictionary._entries;
            _index = 0;
            _count = dictionary._count;
        }

        /// <inheritdoc cref="IEnumerator.MoveNext"/>
        public bool MoveNext()
        {
            if (_count == 0)
            {
                return false;
            }

            _count--;

            var entries = _entries;

            while (entries[_index].next < -1)
            {
                _index++;
            }

            // We need to preemptively increment the current index so that we still correctly keep track
            // of the current position in the dictionary even if the users doesn't access any of the
            // available properties in the enumerator. As this is a possibility, we can't rely on one of
            // them to increment the index before MoveNext is invoked again. We ditch the standard enumerator
            // API surface here to expose the Key/Value properties directly and minimize the memory copies.
            // For the same reason, we also removed the KeyValuePair<TKey, TValue> field here, and instead
            // rely on the properties lazily accessing the target instances directly from the current entry
            // pointed at by the index property (adjusted backwards to account for the increment here).
            _index++;

            return true;
        }

        /// <summary>
        /// Gets the current key.
        /// </summary>
        public TKey Key
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries[_index - 1].key;
        }

        /// <summary>
        /// Gets the current value.
        /// </summary>
        public TValue Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _entries[_index - 1].value!;
        }
    }

    /// <summary>
    /// Throws an <see cref="ArgumentException"/> when trying to load an element with a missing key.
    /// </summary>
    private static void ThrowArgumentExceptionForKeyNotFound(TKey key)
    {
        throw new ArgumentException($"The target key {key} was not present in the dictionary");
    }

    /// <summary>
    /// Throws an <see cref="InvalidOperationException"/> when trying to resize over the maximum capacity.
    /// </summary>
    private static void ThrowInvalidOperationExceptionForMaxCapacityExceeded()
    {
        throw new InvalidOperationException("Max capacity exceeded");
    }
}