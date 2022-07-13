using System.Drawing;
using System.Runtime.CompilerServices;

namespace HexControl.SharedControl.Documents;

public sealed class Marker : IDocumentMarker
{
    private long _offset;
    private long _length;

    public Marker(long offset, long length)
    {
        _offset = offset;
        _length = length;
    }

    public Guid Id { get; set; }

    // Not auto properties for performance reasons
    public long Offset { get => _offset; set => _offset = value; }
    public long Length { get => _length; set => _length = value; }

    public Color? Background { get; set; }
    public Color? Border { get; set; }
    public Color? Foreground { get; set; }
    public bool BehindText { get; set; }
    public ColumnSide Column { get; set; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsVisible(long offset, long length)
    {
        return unchecked(_offset + _length) > offset && _offset < unchecked(offset + length);
    }
}