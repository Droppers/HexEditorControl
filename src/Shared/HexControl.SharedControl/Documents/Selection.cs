using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public record struct Selection
{
    public Selection(long start, long end, ColumnSide column)
    {
        if (end < start)
        {
            throw new ArgumentException("End offset cannot be lower than start offset.", nameof(end));
        }

        Start = start;
        Length = end - start;
        Column = column;
    }
    
    public long Start { get; init; }

    public long Length { get; init; }

    public long End => Start + Length;

    public ColumnSide Column { get; init; }
}