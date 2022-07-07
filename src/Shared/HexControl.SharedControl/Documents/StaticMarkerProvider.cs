using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using HexControl.SharedControl.Documents.Helpers;

namespace HexControl.SharedControl.Documents;

public sealed class StaticMarkerProvider
{
    private readonly List<IntegerColor> _colorBuilder;
    private readonly List<MarkerSequence> _sequenceBuilder;

    private long _offset = -1;

    private MarkerSequence[] _sequences = Array.Empty<MarkerSequence>();

    public StaticMarkerProvider()
    {
        _colorBuilder = new List<IntegerColor>();
        _sequenceBuilder = new List<MarkerSequence>();
    }

    private ref MarkerSequence this[int index]
    {
        get
        {
            ref var data = ref MemoryMarshal.GetArrayDataReference(_sequences);
            return ref Unsafe.Add(ref data, index);
        }
    }

    public IntegerColor? Lookup(long offset)
    {
        if (_sequences.Length is 0)
        {
            return null;
        }

        for (var i = 0; i < _sequences.Length; i++)
        {
            var seq = this[i];
            if (offset >= seq.Offset && offset < seq.Offset + seq.Entries.Length)
            {
                var relativeOffset = offset - seq.Offset;
                return seq[(int)relativeOffset];
            }
        }

        return null;
    }

    public void Add(long offset, long length, IntegerColor color)
    {
        if (_offset + _colorBuilder.Count != offset && _offset is not -1)
        {
            Complete(false);
        }

        if (_offset is -1)
        {
            _offset = offset;
        }

        for (var i = 0; i < length; i++)
        {
            _colorBuilder.Add(color);
        }
    }

    public void Complete(bool final = true)
    {
        var array = _colorBuilder.ToArray();
        _colorBuilder.Clear();

        _sequenceBuilder.Add(new MarkerSequence(_offset, array));
        _offset = -1;

        if (!final)
        {
            return;
        }

        _sequenceBuilder.Sort((a, b) => a.Offset.CompareTo(b.Offset));
        _sequences = _sequenceBuilder.ToArray();

        _colorBuilder.TrimExcess();
        _sequenceBuilder.Clear();
        _sequenceBuilder.TrimExcess();
    }

    private readonly struct MarkerSequence
    {
        public MarkerSequence(long offset, IntegerColor[] entries)
        {
            Offset = offset;
            Entries = entries;
        }

        public long Offset { get; }
        public IntegerColor[] Entries { get; }

        public ref IntegerColor this[int index]
        {
            get
            {
                ref var data = ref MemoryMarshal.GetArrayDataReference(Entries);
                return ref Unsafe.Add(ref data, index);
            }
        }
    }
}