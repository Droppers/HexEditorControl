using System.Drawing;

namespace HexControl.Core;

public interface IDocumentMarker
{
    public Guid Id { get; set; }

    public long Offset { get; set; }
    public long Length { get; set; }

    public Color? Background { get; set; }
    public Color? Border { get; set; }
    public Color? Foreground { get; set; }
    public bool BehindText { get; set; }
    public ColumnSide Column { get; set; }
}

public class Marker : IDocumentMarker
{
    public Marker(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }

    public Guid Id { get; set; }

    public long Offset { get; set; }
    public long Length { get; set; }

    public Color? Background { get; set; }
    public Color? Border { get; set; }
    public Color? Foreground { get; set; }
    public bool BehindText { get; set; }
    public ColumnSide Column { get; set; }
}