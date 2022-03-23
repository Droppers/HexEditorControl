using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System;

namespace HexControl.Core.Buffers.Chunks;

public class FileChunk : ReadOnlyChunk
{
    private readonly MemoryMappedViewAccessor _accessor;
    private readonly FileStream _fileStream;

    public FileChunk(BaseBuffer buffer, FileStream fileStream, MemoryMappedViewAccessor accessor) : base(buffer)
    {
        _fileStream = fileStream;
        _accessor = accessor;
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
        CancellationToken cancellationToken = default)
    {
        _fileStream.Seek(sourceReadOffset, SeekOrigin.Begin);
        await _fileStream.ReadAsync(readBuffer.AsMemory(0, (int)readLength), cancellationToken);
    }

    public override IChunk Clone() =>
        new FileChunk(buffer, _fileStream, _accessor)
        {
            Length = Length,
            SourceOffset = SourceOffset
        };
}