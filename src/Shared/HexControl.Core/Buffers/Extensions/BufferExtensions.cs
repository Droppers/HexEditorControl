using HexControl.Core.Helpers;

namespace HexControl.Core.Buffers.Extensions;

public enum Endianess
{
    Native,
    Big,
    Little
}

public static class BufferExtensions
{
    private static readonly ExactArrayPool<byte> Pool = ExactArrayPool<byte>.Instance;
    
    public static long ReadInt64(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(long) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(long));
        try
        {
            buffer.Read(offset, bytes);
            return BitConverter.ToInt64(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static ulong ReadUInt64(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ulong) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(ulong));
        try
        {
            buffer.Read(offset, bytes);
            return BitConverter.ToUInt64(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static int ReadInt32(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(int) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(int));
        try {
        buffer.Read(offset, bytes);
        return BitConverter.ToInt32(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static uint ReadUInt32(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(uint) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(uint));

        try {
        buffer.Read(offset, bytes);
        return BitConverter.ToUInt32(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static short ReadInt16(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(short));
        try {
        buffer.Read(offset, bytes);
        return BitConverter.ToInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static ushort ReadUInt16(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(ushort) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(ushort));
        try {
        buffer.Read(offset, bytes);
        return BitConverter.ToUInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static char ReadChar(this BaseBuffer buffer, long offset, Endianess endianess = Endianess.Native)
    {
        if (offset + sizeof(short) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(char));
        try {
        buffer.Read(offset, bytes);
        return (char)BitConverter.ToInt16(SwapEndianess(bytes, endianess));
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    public static sbyte ReadByte(this BaseBuffer buffer, long offset)
    {
        if (offset + sizeof(byte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(sbyte));
        try {
        buffer.Read(offset, bytes);
        return (sbyte)bytes[0];
    }
    finally
    {
        Pool.Return(bytes);
    }
    }

    public static byte ReadUByte(this BaseBuffer buffer, long offset)
    {
        if (offset + sizeof(byte) > buffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Cannot read longer than buffer length.");
        }

        var bytes = Pool.Rent(sizeof(byte));
        try
        {
            buffer.Read(offset, bytes);
            return bytes[0];
        }
        finally
        {
            Pool.Return(bytes);
        }
    }

    private static byte[] SwapEndianess(byte[] buffer, Endianess endianess)
    {
        var shouldSwap = BitConverter.IsLittleEndian && endianess is Endianess.Big ||
                         !BitConverter.IsLittleEndian && endianess is Endianess.Little;
        if (shouldSwap)
        {
            Array.Reverse(buffer);
        }

        return buffer;
    }
}