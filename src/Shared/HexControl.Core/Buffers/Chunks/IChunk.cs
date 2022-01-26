namespace HexControl.Core.Buffers.Chunks;

public interface IChunk
{
    public long SourceOffset { get; set; }
    public long Length { get; set; }

    Task<byte[]> ReadAsync(long readOffset, long readLength, CancellationToken cancellationToken = default);
    Task<long> ReadAsync(byte[] readBuffer, long readOffset, long readLength, CancellationToken cancellationToken = default);

    byte[] Read(long readOffset, long readLength);
    long Read(byte[] readBuffer, long readOffset, long readLength);

    IChunk Clone();
}