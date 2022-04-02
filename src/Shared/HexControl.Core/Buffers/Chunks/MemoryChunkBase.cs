namespace HexControl.Core.Buffers.Chunks;

internal abstract class MemoryChunkBase : Chunk
{
    protected MemoryChunkBase(BaseBuffer buffer, byte[] bytes) : base(buffer)
    {
        Bytes = bytes;
        SourceOffset = 0;
        Length = bytes.Length;
    }

    public byte[] Bytes { get; set; }

    protected override void InternalRead(byte[] readBuffer, long sourceReadOffset, long readLength)
    {
        Array.Copy(Bytes, sourceReadOffset, readBuffer, 0, readLength);
    }

    protected override Task InternalReadAsync(byte[] readBuffer, long sourceReadOffset, long readLength,
        CancellationToken cancellationToken = default)
    {
        InternalRead(readBuffer, sourceReadOffset, readLength);
        return Task.CompletedTask;
    }
}