using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public record struct Caret(long Offset, int Nibble, ColumnSide Column);