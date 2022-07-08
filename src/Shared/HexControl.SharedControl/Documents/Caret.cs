using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public class Caret : IEquatable<Caret>
{
    public Caret(long offset, int nibble, ColumnSide column)
    {
        Offset = offset;
        Nibble = nibble;
        Column = column;
    }

    public long Offset { get; }
    public int Nibble { get; }
    public ColumnSide Column { get; }

    public bool Equals(Caret? other) => other is not null && other.Offset == Offset && other.Nibble == Nibble &&
                                        other.Column == Column;

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Caret other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Offset, Nibble, Column);
}