using System.Drawing;

namespace HexControl.Core;

public class Marker
{
    public Marker(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }

    public long Offset { get; internal set; }
    public long Length { get; internal set; }

    public string? Id { get; init; }

    public Color? Background { get; init; }
    public Color? Border { get; init; }
    public Color? Foreground { get; init; }
    public bool BehindText { get; init; }
    public ColumnSide Column { get; init; }
}