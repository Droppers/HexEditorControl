namespace HexControl.Core.Buffers.Chunks;

public abstract class Chunk : IChunk
{
    protected readonly BaseBuffer buffer;

    protected Chunk(BaseBuffer buffer)
    {
        this.buffer = buffer;
    }

    public long SourceOffset { get; set; }
    public long Length { get; set; }

    public async Task<byte[]> ReadAsync(long readOffset, long readLength, CancellationToken cancellationToken)
    {
        (readOffset, readLength) = TranslateOffsetLength(readOffset, readLength);

        var readBuffer = new byte[readLength];
        await InternalReadAsync(readBuffer, readOffset, readLength, cancellationToken);
        return readBuffer;
    }

    public async Task<long> ReadAsync(byte[] readBuffer, long readOffset, long readLength,
        CancellationToken cancellationToken)
    {
        (readOffset, readLength) = TranslateOffsetLength(readOffset, readLength);

        await InternalReadAsync(readBuffer, readOffset, readLength, cancellationToken);
        return readLength;
    }

    public byte[] Read(long readOffset, long readLength)
    {
        (readOffset, readLength) = TranslateOffsetLength(readOffset, readLength);

        var readBuffer = new byte[readLength];
        InternalRead(readBuffer, readOffset, readLength);
        return readBuffer;
    }

    public long Read(byte[] readBuffer, long readOffset, long readLength)
    {
        (readOffset, readLength) = TranslateOffsetLength(readOffset, readLength);

        InternalRead(readBuffer, readOffset, readLength);
        return readLength;
    }

    public abstract IChunk Clone();

    private (long offset, long length) TranslateOffsetLength(long readOffset, long readLength)
    {
        var translation = readOffset;
        var translatedOffset = SourceOffset + translation;
        var translatedLength = Math.Min(Length - translation, readLength);
        return (translatedOffset, translatedLength);
    }

    protected abstract Task InternalReadAsync(byte[] readBuffer, long sourceReadOffset, long readLength,
        CancellationToken cancellationToken);

    protected abstract void InternalRead(byte[] readBuffer, long sourceReadOffset, long readLength);
}