namespace HexControl.Core.Buffers.Chunks;

internal class MemoryChunk : Chunk
{
    public MemoryChunk(BaseBuffer buffer, byte[] bytes) : base(buffer)
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

    public override Chunk Clone()
    {
        var cloneBuffer = new byte[Bytes.Length];
        Buffer.BlockCopy(Bytes, 0, cloneBuffer, 0, Bytes.Length);
        return new MemoryChunk(buffer, cloneBuffer)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
    }
}