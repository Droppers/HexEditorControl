namespace HexControl.Buffers.Chunks;

internal class ImmutableMemoryChunk : MemoryChunkBase, IImmutableChunk
{
    public ImmutableMemoryChunk(BaseBuffer buffer, byte[] bytes) : base(buffer, bytes) { }

    public override IChunk Clone() =>
        new ImmutableMemoryChunk(buffer, Bytes)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
}