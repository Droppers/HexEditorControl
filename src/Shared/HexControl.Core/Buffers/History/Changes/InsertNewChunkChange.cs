using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Buffers.History.Changes;

public class InsertNewChunkChange : IBufferChange
{
    private readonly bool _before;
    private readonly IChunk _chunk;

    public InsertNewChunkChange(IChunk chunk, bool before)
    {
        _chunk = chunk;
        _before = before;
    }

    public IBufferChange Apply(BaseBuffer buffer, LinkedListNode<IChunk>? contextNode)
    {
        // Cloning is important, otherwise the chunk that will be reverted can be modified which results in an invalid state.
        var chunk = _chunk.Clone();

        if (contextNode == null)
        {
            buffer.Chunks.AddFirst(chunk);
        }
        else if (_before)
        {
            buffer.Chunks.AddBefore(contextNode, chunk);
        }
        else
        {
            buffer.Chunks.AddAfter(contextNode, chunk);
        }

        buffer.Length += chunk.Length;
        return this;
    }

    public IBufferChange Revert(BaseBuffer buffer, LinkedListNode<IChunk>? contextNode)
    {
        _ = contextNode ?? throw new ArgumentNullException(nameof(contextNode));

        buffer.Chunks.Remove(contextNode);

        buffer.Length -= _chunk.Length;
        return this;
    }
}