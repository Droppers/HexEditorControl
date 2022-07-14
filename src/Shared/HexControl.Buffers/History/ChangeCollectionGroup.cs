namespace HexControl.Buffers.History;

internal record struct ChangeCollectionGroup(IReadOnlyList<ChangeCollection> Collections)
{
    public ChangeCollectionGroup(ChangeCollection collection) : this(new[] {collection})
    {
    }
}