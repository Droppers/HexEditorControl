using System.IO.MemoryMappedFiles;
using System.Runtime.CompilerServices;

namespace HexControl.Buffers.Chunks;

internal class FileChunk : Chunk, IImmutableChunk
{
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly FileStream _fileStream;

    public long SourceOffset { get; set; }

    public FileChunk(ByteBuffer byteBuffer, FileStream fileStream, MemoryMappedViewAccessor accessor) : base(byteBuffer)
    {
        _fileStream = fileStream;
        _accessor = accessor;
    }

    public override IChunk Clone() =>
        new FileChunk(byteBuffer, _fileStream, _accessor)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };

    protected override long InternalRead(Span<byte> buffer, long offset)
    {
        _fileStream.Seek(offset + SourceOffset, SeekOrigin.Begin);
        var bytesRead = _fileStream.Read(buffer);

        if (bytesRead < buffer.Length)
        {
            throw new InvalidOperationException($"File chunk returned unexpected number of bytes '{bytesRead}'.");
        }

        return buffer.Length;
    }

    protected override async Task<long> InternalReadAsync(Memory<byte> buffer, long offset, CancellationToken cancellationToken = default)
    {
        _fileStream.Seek(offset + SourceOffset, SeekOrigin.Begin);
        var bytesRead = await _fileStream.ReadAsync(buffer, cancellationToken);

        if (bytesRead < buffer.Length)
        {
            throw new InvalidOperationException($"File chunk returned unexpected number of bytes '{bytesRead}'.");
        }

        return buffer.Length;
    }
}