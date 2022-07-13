using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

internal class RemoveFromImmutableChange : IChunkChange<IImmutableChunk>
{
    private readonly long _removeLength;
    private readonly long _removeOffset;

    public RemoveFromImmutableChange(long removeOffset, long removeLength)
    {
        _removeOffset = removeOffset;
        _removeLength = removeLength;
    }

    public IChunkChange<IImmutableChunk> Apply(ByteBuffer buffer, LinkedListNode<IChunk> contextNode,
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

    public IChunkChange<IImmutableChunk> Revert(ByteBuffer buffer, LinkedListNode<IChunk> contextNode,
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