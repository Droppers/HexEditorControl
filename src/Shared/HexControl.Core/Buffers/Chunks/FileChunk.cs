using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace HexControl.Core.Buffers.Chunks;

public class FileChunk : ReadOnlyChunk
{
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly MemoryMappedFile _file;
    private readonly FileStream _fileStream;
    private readonly MemoryMappedViewStream _stream;

    public FileChunk(BaseBuffer buffer, FileStream fileStream, MemoryMappedFile file, MemoryMappedViewAccessor accessor,
        MemoryMappedViewStream stream) : base(buffer)
    {
        _fileStream = fileStream;
        _file = file;
        _accessor = accessor;
        _stream = stream;
    }

    protected override unsafe void InternalRead(byte[] readBuffer, long sourceReadOffset, long readLength)
    {
        var ptr = (byte*)0;
        _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref ptr);
        try
        {
            Marshal.Copy(IntPtr.Add(new IntPtr(ptr), (int)sourceReadOffset), readBuffer, 0, (int)readLength);
        }
        finally
        {
            _accessor.SafeMemoryMappedViewHandle.ReleasePointer();
        }
    }

    // TODO: cancellation support
    protected override async Task InternalReadAsync(byte[] readBuffer, long sourceReadOffset, long readLength,
        CancellationToken cancellationToken)
    {
        _fileStream.Seek(sourceReadOffset, SeekOrigin.Begin);
        await _fileStream.ReadAsync(readBuffer, 0, (int)readLength, cancellationToken);
    }

    public override IChunk Clone() =>
        new FileChunk(buffer, _fileStream, _file, _accessor, _stream)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
}