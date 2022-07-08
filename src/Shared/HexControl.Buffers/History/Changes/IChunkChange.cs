using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

public interface IChange { }

public interface IChunkChange<in TChunk> : IChange
{
    IChunkChange<TChunk> Apply(ByteBuffer buffer, LinkedListNode<IChunk> contextNode, TChunk chunk);
    IChunkChange<TChunk> Revert(ByteBuffer buffer, LinkedListNode<IChunk> contextNode, TChunk chunk);
}

public interface IBufferChange : IChange
{
    IBufferChange Apply(ByteBuffer buffer, LinkedListNode<IChunk>? contextNode);
    IBufferChange Revert(ByteBuffer buffer, LinkedListNode<IChunk>? contextNode);
}