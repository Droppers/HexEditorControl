using System.Drawing;

namespace HexControl.Core;

public interface IDocumentMarker
{
    public Guid Id { get; set; }

    public long Offset { get; set; }
    public long Length { get; set; }

    public Color? Background { get; init; }
    public Color? Border { get; init; }
    public Color? Foreground { get; init; }
    public bool BehindText { get; init; }
    public ColumnSide Column { get; init; }
}

public class Marker : IDocumentMarker
{
    public Marker(long offset, long length)
    {
        Offset = offset;
        Length = length;
    }

    public Guid Id { get; set; }

    public virtual long Offset { get; set; }
    public virtual long Length { get; set; }
    
    public Color? Background { get; init; }
    public Color? Border { get; init; }
    public Color? Foreground { get; init; }
    public bool BehindText { get; init; }
    public ColumnSide Column { get; init; }
}