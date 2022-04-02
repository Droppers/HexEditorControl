using JetBrains.Annotations;

namespace HexControl.Core.Buffers;

// See the following link for implementation instructions: https://docs.microsoft.com/en-us/dotnet/api/system.io.stream#notes-to-implementers
[PublicAPI]
public class BufferStream : Stream
{
    private readonly BaseBuffer _buffer;

    private readonly byte[] _byteBuffer;
    private long _position;

    public BufferStream(BaseBuffer buffer)
    {
        _buffer = buffer;
        _byteBuffer = new byte[1];
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;

    public override bool CanWrite => !_buffer.IsReadOnly;

    public override long Length => _buffer.Length;

    public override long Position
    {
        get => _position;
        set => Seek(value, SeekOrigin.Begin);
    }

    public override void Flush()
    {
        _buffer.SaveAsync().GetAwaiter().GetResult();
    }

    public override Task FlushAsync(CancellationToken cancellationToken) => _buffer.SaveAsync(cancellationToken);

    public override int ReadByte()
    {
        var readLength = Read(_byteBuffer, 0, 1);
        if (readLength <= 0)
        {
            throw new InvalidOperationException("Could not read byte.");
        }

        return _byteBuffer[0];
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset is not 0)
        {
            throw new NotSupportedException("Reading data to a specific offset in buffer not yet supported.");
        }

        var bytesRead = (int)_buffer.Read(buffer, offset, count);
        _position += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (offset is not 0)
        {
            throw new NotSupportedException("Reading data to a specific offset in buffer not yet supported.");
        }

        var bytesRead = (int)await _buffer.ReadAsync(buffer, _position, count, cancellationToken: cancellationToken);
        _position += bytesRead;
        return bytesRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        _position = origin switch
        {
            SeekOrigin.Begin => offset,
            SeekOrigin.End => Length - Position,
            _ => Position + offset
        };

        return _position;
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException("BufferStream length cannot be changed yet.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        // TODO: implement write
        // We do not have support for the given parameters yet
        throw new NotImplementedException();
    }
}