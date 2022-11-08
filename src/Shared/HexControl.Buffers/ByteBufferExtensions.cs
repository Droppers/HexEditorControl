using HexControl.Buffers.Numerics;
using JetBrains.Annotations;

namespace HexControl.Buffers;

[PublicAPI]
public enum Endianess
{
    Native,
    Big,
    Little
}

[PublicAPI]
public static class ByteBufferExtensions
{
    public static Int128 ReadInt128(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + Int128.Size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[Int128.Size];
        buffer.Read(bytes, offset);
        SwapEndianess(bytes, endianess);
        var a = BitConverter.ToInt64(bytes.Slice(0, Int128.Size / 2));
        var b = BitConverter.ToInt64(bytes.Slice(Int128.Size / 2, Int128.Size / 2));
        return new Int128(a, b);
    }

    public static UInt128 ReadUInt128(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + UInt128.Size > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }
        
        Span<byte> bytes = stackalloc byte[UInt128.Size];
        buffer.Read(bytes, offset);
        SwapEndianess(bytes, endianess);
        var a = BitConverter.ToUInt64(bytes.Slice(0, UInt128.Size / 2));
        var b = BitConverter.ToUInt64(bytes.Slice(UInt128.Size / 2, UInt128.Size / 2));
        return new UInt128(a, b);
    }

    public static long ReadInt64(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(long) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(long)];
        buffer.Read(bytes, offset);
        return BitConverter.ToInt64(SwapEndianess(bytes, endianess));
    }

    public static ulong ReadUInt64(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ulong) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(ulong)];
        buffer.Read(bytes, offset);
        return BitConverter.ToUInt64(SwapEndianess(bytes, endianess));
    }

    public static int ReadInt32(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(int) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(int)];
        buffer.Read(bytes, offset);
        return BitConverter.ToInt32(SwapEndianess(bytes, endianess));
    }

    public static uint ReadUInt32(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(uint) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(uint)];
        buffer.Read(bytes, offset);
        return BitConverter.ToUInt32(SwapEndianess(bytes, endianess));
    }

    public static short ReadInt16(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(short)];
        buffer.Read(bytes, offset);
        return BitConverter.ToInt16(SwapEndianess(bytes, endianess));
    }

    public static ushort ReadUInt16(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ushort) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(ushort)];
        buffer.Read(bytes, offset);
        return BitConverter.ToUInt16(SwapEndianess(bytes, endianess));
    }

    public static char ReadChar(this ByteBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(char)];
        buffer.Read(bytes, offset);
        return (char)BitConverter.ToInt16(SwapEndianess(bytes, endianess)); 
    }

    public static byte ReadUInt8(this ByteBuffer buffer, long offset)
    {
        if (offset + sizeof(byte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(byte)];
        buffer.Read(bytes, offset);
        return bytes[0];
    }

    public static sbyte ReadInt8(this ByteBuffer buffer, long offset)
    {
        if (offset + sizeof(sbyte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read outside the buffer.");
        }

        Span<byte> bytes = stackalloc byte[sizeof(sbyte)];
        buffer.Read(bytes, offset);
        return (sbyte)bytes[0];
    }

    public static IEnumerable<long> FindAll(this ByteBuffer buffer, byte[] pattern, long offset,
        CancellationToken cancellationToken = default)
    {
        return FindAll(buffer, pattern, offset, null, default, cancellationToken);
    }

    public static IEnumerable<long> FindAll(this ByteBuffer buffer, byte[] pattern, long offset, FindOptions options,
        CancellationToken cancellationToken = default)
    {
        return FindAll(buffer, pattern, offset, null, options, cancellationToken);
    }

    public static IEnumerable<long> FindAll(this ByteBuffer buffer, byte[] pattern, long offset, long length,
        CancellationToken cancellationToken = default)
    {
        return FindAll(buffer, pattern, offset, length, default, cancellationToken);
    }

    public static IEnumerable<long> FindAll(this ByteBuffer buffer, byte[] pattern, long offset, long? length,
        FindOptions options, CancellationToken cancellationToken = default)
    {
        length ??= buffer.Length;
        var lastOffset = offset;
        var searchedLength = 0L;

        while (length - searchedLength > 0)
        {
            var currentOffset = buffer.Find(pattern, lastOffset, length - searchedLength, options, cancellationToken);
            if (currentOffset is -1)
            {
                yield break;
            }

            yield return currentOffset;

            long newLastOffset;
            if (options.Backward)
            {
                newLastOffset = currentOffset - pattern.Length <= 0
                    ? buffer.Length - 1
                    : currentOffset - pattern.Length;
                searchedLength += newLastOffset > lastOffset
                    ? lastOffset + 1
                    : lastOffset - newLastOffset;
            }
            else
            {
                newLastOffset = currentOffset + pattern.Length;
                searchedLength += newLastOffset < lastOffset
                    ? newLastOffset + ((buffer.Length) - lastOffset)
                    : newLastOffset - lastOffset;
            }

            lastOffset = newLastOffset;
        }
    }

    private static Span<byte> SwapEndianess(Span<byte> buffer, Endianess endianess)
    {
        var shouldSwap = BitConverter.IsLittleEndian && endianess is Endianess.Big ||
                         !BitConverter.IsLittleEndian && endianess is Endianess.Little;
        if (shouldSwap)
        {
            buffer.Reverse();
        }

        return buffer;
    }
}