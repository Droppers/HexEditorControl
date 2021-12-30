namespace HexControl.Core.Buffers.Chunks;

public abstract class VirtualChunk : Chunk
{
    protected VirtualChunk(BaseBuffer buffer) : base(buffer) { }
}