using JetBrains.Annotations;

namespace HexControl.Buffers.Chunks;

[PublicAPI]
public abstract class Chunk : IChunk
{
    protected readonly ByteBuffer byteBuffer;

    protected Chunk(ByteBuffer byteBuffer)
    {
        this.byteBuffer = byteBuffer;
    }

    public long Length { get; set; }
    
    public async Task<long> ReadAsync(Memory<byte> buffer, long offset, CancellationToken cancellationToken = default)
    {
        if (offset + buffer.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length, $"Offset with length '{offset + buffer.Length}' exceed chunk length '{Length}'.");
        }

        return await InternalReadAsync(buffer, offset, cancellationToken);
    }
    
    public long Read(Span<byte> buffer, long offset)
    {
        if (offset + buffer.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(buffer), buffer.Length, $"Offset with length '{offset + buffer.Length}' exceed chunk length '{Length}'.");
        }

        return InternalRead(buffer, offset);
    }

    public abstract IChunk Clone();
    
    protected abstract Task<long> InternalReadAsync(Memory<byte> buffer, long offset,
        CancellationToken cancellationToken = default);

    protected abstract long InternalRead(Span<byte> buffer, long offset);
}