using System.Drawing;
using JetBrains.Annotations;

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
    public MarkerColumn Column { get; set; }

    public bool IsVisible(long offset, long length);
}

[PublicAPI]
public enum MarkerColumn
{
    Hex,
    Text,
    HexText
}