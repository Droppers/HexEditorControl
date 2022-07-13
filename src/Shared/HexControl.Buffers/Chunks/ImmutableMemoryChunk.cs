namespace HexControl.Buffers.Chunks;

internal class ImmutableMemoryChunk : Chunk, IImmutableChunk
{
    private readonly byte[] _bytes;
    public long SourceOffset { get; set; }

    public ImmutableMemoryChunk(ByteBuffer byteBuffer, byte[] bytes) : base(byteBuffer)
    {
        _bytes = bytes;
        Length = bytes.Length;
    }

    protected override long InternalRead(Span<byte> buffer, long offset)
    {
        new Span<byte>(_bytes, (int)(offset + SourceOffset), buffer.Length).CopyTo(buffer);
        return buffer.Length;
    }

    protected override Task<long> InternalReadAsync(Memory<byte> buffer, long offset,
        CancellationToken cancellationToken = default)
    {
        new Span<byte>(_bytes, (int)(offset + SourceOffset), buffer.Length).CopyTo(buffer.Span);
        return Task.FromResult((long)buffer.Length);
    }

    public override IChunk Clone() =>
        new ImmutableMemoryChunk(byteBuffer, _bytes)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
}