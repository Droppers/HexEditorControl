using System;
using System.Collections.Concurrent;
using System.Threading;

namespace HexControl.PatternLanguage.Helpers;

internal sealed class StringDictionary
{
    private readonly ConcurrentDictionary<int, uint> _backward;

    private readonly ConcurrentDictionary<uint, string> _forward;

    private readonly Func<StringDictionary, string, uint>? _reservedSpaceHandler;

    private uint _indexCount;

    public StringDictionary(uint reservedSpace, Func<StringDictionary, string, uint>? reservedSpaceHandler = null)
    {
        NullIndex = reservedSpace;
        _reservedSpaceHandler = reservedSpaceHandler;
        _indexCount = (uint)((short)NullIndex + 1);

        _forward = new ConcurrentDictionary<uint, string>();
        _backward = new ConcurrentDictionary<int, uint>();
    }

    public uint NullIndex { get; }
    public int FirstUsableIndex => (int)NullIndex + 1;

    public uint AddOrGet(string? str)
    {
        if (str is null)
        {
            return NullIndex;
        }

        var reservedIndex = _reservedSpaceHandler?.Invoke(this, str) ?? NullIndex;
        if (reservedIndex != NullIndex)
        {
            return reservedIndex;
        }

        if (_backward.TryGetValue(str.GetHashCode(), out var storedIndex))
        {
            return storedIndex;
        }

        var index = Interlocked.Increment(ref _indexCount);
        _forward.TryAdd(index, str);
        _backward.TryAdd(str.GetHashCode(), index);
        return index;
    }

    public string? Get(uint index) => _forward.TryGetValue(index, out var value) ? value : null;
}