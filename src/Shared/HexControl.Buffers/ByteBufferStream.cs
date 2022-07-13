using JetBrains.Annotations;

namespace HexControl.Buffers;

// TODO: Update whenever ByteBuffer accepts Span and Memory instead of byte arrays
// See the following link for implementation instructions: https://docs.microsoft.com/en-us/dotnet/api/system.io.stream#notes-to-implementers
[PublicAPI]
public class ByteBufferStream : Stream
{
    private readonly ByteBuffer _buffer;

    private readonly byte[] _readBuffer;
    private long _position;

    public ByteBufferStream(ByteBuffer buffer)
    {
        _buffer = buffer;
        _readBuffer = new byte[1];
    }

    public override bool CanRead => true;
    public override bool CanSeek => true;

    public override bool CanWrite => !_buffer.IsReadOnly && !_buffer.Locked;

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
        var readLength = Read(_readBuffer, 0, 1);
        if (readLength <= 0)
        {
            throw new InvalidOperationException("Could not read byte.");
        }

        return _readBuffer[0];
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset is not 0)
        {
            throw new NotSupportedException("Reading data to a specific offset in byteBuffer not yet supported.");
        }

        var bytesRead = (int)_buffer.Read(buffer.AsSpan(0, count), offset);
        _position += bytesRead;
        return bytesRead;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        if (offset is not 0)
        {
            throw new NotSupportedException("Reading data to a specific offset in byteBuffer not yet supported.");
        }

        var bytesRead = (int)await _buffer.ReadAsync(buffer.AsMemory(count), _position, cancellationToken: cancellationToken);
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
        throw new NotSupportedException("ByteBufferStream length cannot be changed yet.");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var copy = new byte[count];
        new Span<byte>(buffer, offset, count).CopyTo(copy.AsSpan());

        _buffer.Write(_position, copy);
    }

    public override void WriteByte(byte value)
    {
        _buffer.Write(_position, value);
    }

    public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        var copy = new byte[count];
        new Span<byte>(buffer, offset, count).CopyTo(copy.AsSpan());

        await _buffer.WriteAsync(_position, copy);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer,
        CancellationToken cancellationToken = new CancellationToken())
    {
        var copy = new byte[buffer.Length];
        buffer.Span.CopyTo(copy.AsSpan());

        await _buffer.WriteAsync(_position, copy);
    }
}