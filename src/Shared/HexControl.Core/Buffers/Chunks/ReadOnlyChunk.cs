namespace HexControl.Core.Buffers.Chunks;

public abstract class ReadOnlyChunk : Chunk
{
    protected ReadOnlyChunk(BaseBuffer buffer) : base(buffer) { }
}