using System.Drawing;

namespace HexControl.SharedControl.Documents;

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