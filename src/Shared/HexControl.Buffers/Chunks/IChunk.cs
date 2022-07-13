using JetBrains.Annotations;

namespace HexControl.Buffers.Chunks;

[PublicAPI]
public interface IChunk
{
    public long Length { get; set; }
    
    Task<long> ReadAsync(Memory<byte> buffer, long offset, CancellationToken cancellationToken = default);

    long Read(Span<byte> buffer, long offset);

    IChunk Clone();
}