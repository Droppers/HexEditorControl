namespace HexControl.Buffers.Chunks;

internal class MemoryChunk : MemoryChunkBase
{
    public MemoryChunk(ByteBuffer buffer, byte[] bytes) : base(buffer, bytes) { }

    public override IChunk Clone()
    {
        var cloneBuffer = new byte[Bytes.LongLength];
        Array.Copy(Bytes, 0, cloneBuffer, 0, Bytes.LongLength);
        return new MemoryChunk(buffer, cloneBuffer)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
    }
}