using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public record struct Selection
{
    public Selection(long start, long length, ActiveColumn column)
    {
        Start = start;
        Length = length;
        Column = column;
    }
    
    public long Start { get; init; }

    public long Length { get; init; }

    public long End => Start + Length;

    public ActiveColumn Column { get; init; }
}