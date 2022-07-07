namespace HexControl.SharedControl.Documents;

public enum ColumnSide
{
    Both,
    Left,
    Right
}

public class Selection : IEquatable<Selection>
{
    public Selection(long start, long end, ColumnSide column)
    {
        if (end < start)
        {
            throw new ArgumentException(nameof(end));
        }

        Start = start;
        End = end;
        Column = column;
    }

    // Used for rendering purposes
    internal long Start { get; }
    internal long End { get; }

    public long Length => End - Start;

    public ColumnSide Column { get; }

    public bool Equals(Selection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Start == other.Start && End == other.End && Column == other.Column;
    }

    public override bool Equals(object? other)
    {
        if (other is null)
        {
            return false;
        }

        return ReferenceEquals(this, other) || Equals(other as Selection);
    }

    public override int GetHashCode() => HashCode.Combine(Start, Length, Column);
}