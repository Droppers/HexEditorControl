using HexControl.Buffers.Chunks;

namespace HexControl.Buffers.History.Changes;

public interface IChange { }

public interface IChunkChange<in TChunk> : IChange
{
    IChunkChange<TChunk> Apply(BaseBuffer buffer, LinkedListNode<IChunk> contextNode, TChunk chunk);
    IChunkChange<TChunk> Revert(BaseBuffer buffer, LinkedListNode<IChunk> contextNode, TChunk chunk);
}

public interface IBufferChange : IChange
{
    IBufferChange Apply(BaseBuffer buffer, LinkedListNode<IChunk>? contextNode);
    IBufferChange Revert(BaseBuffer buffer, LinkedListNode<IChunk>? contextNode);
}