using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public enum NewCaretLocation
{
    Current,
    SelectionEnd,
    SelectionStart
}