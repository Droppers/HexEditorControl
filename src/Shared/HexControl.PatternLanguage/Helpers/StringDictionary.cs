using System;
using System.Collections.Generic;

namespace HexControl.PatternLanguage.Helpers;

internal sealed class StringDictionary
{
    private readonly Dictionary<string, uint> _backward;

    private readonly Dictionary<uint, string> _forward;

    private readonly Func<StringDictionary, string, uint>? _reservedSpaceHandler;
    private uint _indexCount;

    public StringDictionary(uint reservedSpace, Func<StringDictionary, string, uint>? reservedSpaceHandler = null)
    {
        NullIndex = reservedSpace;
        _reservedSpaceHandler = reservedSpaceHandler;
        _indexCount = (uint)((short)NullIndex + 1);

        _forward = new Dictionary<uint, string>();
        _backward = new Dictionary<string, uint>();
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

        if (_backward.TryGetValue(str, out var storedIndex))
        {
            return storedIndex;
        }

        var index = _indexCount++;
        _forward.Add(index, str);
        _backward.Add(str, index);
        return index;
    }

    public string? Get(uint index) => _forward.TryGetValue(index, out var value) ? value : null;
}