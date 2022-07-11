namespace HexControl.Buffers.Chunks;

internal class MemoryChunk : Chunk
{
    public MemoryChunk(ByteBuffer byteBuffer, byte[] bytes) : base(byteBuffer)
    {
        Bytes = bytes;
        Length = Bytes.Length;
    }

    public byte[] Bytes { get; set; }

    protected override long InternalRead(Span<byte> buffer, long offset)
    {
        new Span<byte>(Bytes, (int)offset, buffer.Length).CopyTo(buffer);
        return buffer.Length;
    }

    protected override Task<long> InternalReadAsync(Memory<byte> buffer, long offset,
        CancellationToken cancellationToken = default)
    {
        new Span<byte>(Bytes, (int)offset, buffer.Length).CopyTo(buffer.Span);
        return Task.FromResult((long)buffer.Length);
    }

    public override IChunk Clone()
    {
        var clonedBuffer = new byte[Bytes.Length];
        InternalRead(clonedBuffer, 0);

        return new MemoryChunk(byteBuffer, clonedBuffer)
        {
            Length = Length
        };
    }
}