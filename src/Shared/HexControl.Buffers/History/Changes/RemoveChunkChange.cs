using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

internal class RemoveChunkChange : IBufferChange
{
    private readonly bool _atStart;
    private IChunk? _removedChunk;

    public RemoveChunkChange(bool atStart)
    {
        _atStart = atStart;
    }

    public IBufferChange Apply(ByteBuffer buffer, LinkedListNode<IChunk>? contextNode)
    {
        if (contextNode is null)
        {
            throw new InvalidOperationException("contextNode cannot be null.");
        }

        _removedChunk = contextNode.Value.Clone();
        buffer.Chunks.Remove(contextNode);

        buffer.Length -= _removedChunk.Length;
        return this;
    }

    public IBufferChange Revert(ByteBuffer buffer, LinkedListNode<IChunk>? contextNode)
    {
        if (contextNode is null)
        {
            throw new ArgumentNullException(nameof(contextNode));
        }

        if (_removedChunk is null)
        {
            throw new InvalidOperationException("There is no chunk to be restored.");
        }

        if (_atStart && contextNode.Previous is null)
        {
            buffer.Chunks.AddFirst(_removedChunk.Clone());
        }
        else
        {
            var targetNode = contextNode;
            buffer.Chunks.AddAfter(targetNode, _removedChunk.Clone());
        }

        buffer.Length += _removedChunk.Length;
        return this;
    }
}