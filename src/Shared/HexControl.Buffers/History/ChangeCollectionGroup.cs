namespace HexControl.Buffers.History;

internal record struct ChangeCollectionGroup(IReadOnlyList<ChangeCollection> Collections)
{
    public static readonly ChangeCollectionGroup Empty = new();

    public ChangeCollectionGroup(ChangeCollection collection) : this(new[] {collection})
    {
    }
}