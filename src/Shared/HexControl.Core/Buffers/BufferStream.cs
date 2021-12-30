namespace HexControl.Core.Buffers;

public class BufferStream : Stream
{
    private readonly BaseBuffer _buffer;

    public BufferStream(BaseBuffer buffer)
    {
        _buffer = buffer;
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;

    public override bool CanWrite { get; } // TODO: determine from buffer

    public override long Length => _buffer.Length;
    public override long Position { get; set; }

    public override void Flush()
    {
        throw new NotSupportedException("BufferStream cannot be flushed.");
    }

    public override int Read(byte[] buffer, int offset, int count) =>
        (int)_buffer.Read(offset, buffer); // TODO: COUNT

    public override async Task<int>
        ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        (int)await _buffer.ReadAsync(offset, buffer);

    public override long Seek(long offset, SeekOrigin origin)
    {
        var newPosition = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.End => Length - Position,
            _ => Position + offset
        };

        return newPosition;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("BufferStream length cannot be changed.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _buffer.Write(offset, buffer); // TODO: COUNT
        throw new NotImplementedException();
    }
}