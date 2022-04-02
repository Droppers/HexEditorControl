using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Buffers.History.Changes;

internal class RemoveFromImmutableChange : IChunkChange<IImmutableChunk>
{
    private readonly long _removeLength;
    private readonly long _removeOffset;

    public RemoveFromImmutableChange(long removeOffset, long removeLength)
    {
        _removeOffset = removeOffset;
        _removeLength = removeLength;
    }

    public IChunkChange<IImmutableChunk> Apply(BaseBuffer buffer, LinkedListNode<IChunk> contextNode,
        IImmutableChunk chunk)
    {
        if (_removeOffset > 0)
        {
            chunk.Length -= _removeLength;
        }
        else
        {
            chunk.SourceOffset += _removeLength;
            chunk.Length -= _removeLength;
        }

        buffer.Length -= _removeLength;
        return this;
    }

    public IChunkChange<IImmutableChunk> Revert(BaseBuffer buffer, LinkedListNode<IChunk> contextNode,
        IImmutableChunk chunk)
    {
        if (_removeOffset > 0)
        {
            chunk.Length += _removeLength;
        }
        else
        {
            chunk.SourceOffset -= _removeLength;
            chunk.Length += _removeLength;
        }

        buffer.Length += _removeLength;
        return this;
    }
}