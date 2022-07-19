using System.Drawing;

namespace HexControl.SharedControl.Documents;

public interface IDocumentMarker
{
    Guid Id { get; set; }

    long Offset { get; set; }
    long Length { get; set; }

    Color? Background { get; set; }
    Color? Border { get; set; }
    Color? Foreground { get; set; }
    bool BehindText { get; set; }
    MarkerColumn Column { get; set; }

    bool IsVisible(long offset, long length);

    void ChangeMarkerOffsetAndLength(long newOffset, long newLength);
}