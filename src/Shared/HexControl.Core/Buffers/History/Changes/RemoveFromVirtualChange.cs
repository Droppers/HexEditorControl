using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Buffers.History.Changes;

internal class RemoveFromVirtualChange : IChunkChange<ReadOnlyChunk>
{
    private readonly long _removeLength;
    private readonly long _removeOffset;

    public RemoveFromVirtualChange(long removeOffset, long removeLength)
    {
        _removeOffset = removeOffset;
        _removeLength = removeLength;
    }

    public IChunkChange<ReadOnlyChunk> Apply(BaseBuffer buffer, LinkedListNode<IChunk> contextNode, ReadOnlyChunk chunk)
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

    public IChunkChange<ReadOnlyChunk> Revert(BaseBuffer buffer, LinkedListNode<IChunk> contextNode,
        ReadOnlyChunk chunk)
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