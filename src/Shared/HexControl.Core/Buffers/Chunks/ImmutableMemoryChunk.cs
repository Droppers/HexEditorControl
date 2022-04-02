namespace HexControl.Core.Buffers.Chunks;

internal class ImmutableMemoryChunk : MemoryChunkBase, IImmutableChunk
{
    public ImmutableMemoryChunk(BaseBuffer buffer, byte[] bytes) : base(buffer, bytes) { }

    public override IChunk Clone() =>
        //var cloneBuffer = new byte[Bytes.LongLength];
        //Array.Copy(Bytes, 0, cloneBuffer, 0, Bytes.LongLength);
        new ImmutableMemoryChunk(buffer, Bytes)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
}