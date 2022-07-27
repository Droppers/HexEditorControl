using System.Runtime.CompilerServices;
using System.Text;

namespace HexControl.SharedControl.Characters;

public enum NumberType
{
    Int16,
    Int32,
    Int64,
    UInt16,
    UInt32,
    UInt64,
    Float,
    Double
}

public class NumberCharacterSet : CharacterSet, IStringConvertible
{
    private readonly NumberType _type;
    private readonly bool _reverseBytes;

    private static int GetSize(NumberType type)
    {
        return type switch
        {
            NumberType.Int16 or NumberType.UInt16 => 2,
            NumberType.Int32 or NumberType.UInt32 => 4,
            NumberType.Int64 or NumberType.UInt64 => 8,
            NumberType.Float => 4,
            NumberType.Double => 8,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static int GetWidth(NumberType type)
    {
        return type switch
        {
            NumberType.Int16 => 6,
            NumberType.UInt16 => 6,
            NumberType.Int32 => 11,
            NumberType.UInt32 => 10,
            NumberType.Int64 => 20,
            NumberType.UInt64 => 20,
            NumberType.Float => 14,
            NumberType.Double => 24,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    public NumberCharacterSet(NumberType type, bool reverseBytes = false)
    {
        _type = type;
        _reverseBytes = reverseBytes;
        Groupable = true;
        Width = GetWidth(type);
        ByteWidth = GetSize(type);
    }

    public override unsafe int GetCharacters(ReadOnlySpan<byte> bytes, Span<char> destBuffer)
    {
        Span<char> tempBuffer = stackalloc char[Width];

        if (TryFormat(bytes, _type, tempBuffer, _reverseBytes, out var charsWritten))
        {
            tempBuffer.Slice(0, charsWritten).CopyTo(destBuffer.Slice(Width - charsWritten));
            destBuffer.Slice(0, Width - charsWritten).Fill(' ');
            return Width;
        }

        return Width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe bool TryFormat(ReadOnlySpan<byte> bytes, NumberType type, Span<char> destBuffer, bool reverse, out int charsWritten)
    {
        if (reverse)
        {
            Span<byte> reversedBytes = stackalloc byte[bytes.Length];
            bytes.CopyTo(reversedBytes);
            reversedBytes.Reverse();
            
            return type switch
            {
                NumberType.Float => BitConverter.ToSingle(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.Double => BitConverter.ToDouble(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.Int16 => BitConverter.ToInt16(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.Int32 => BitConverter.ToInt32(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.Int64 => BitConverter.ToInt64(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.UInt16 => BitConverter.ToUInt16(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.UInt32 => BitConverter.ToUInt32(reversedBytes).TryFormat(destBuffer, out charsWritten),
                NumberType.UInt64 => BitConverter.ToUInt64(reversedBytes).TryFormat(destBuffer, out charsWritten),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
            };
        }

        return type switch
        {
            NumberType.Float => BitConverter.ToSingle(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.Double => BitConverter.ToDouble(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.Int16 => BitConverter.ToInt16(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.Int32 => BitConverter.ToInt32(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.Int64 => BitConverter.ToInt64(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.UInt16 => BitConverter.ToUInt16(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.UInt32 => BitConverter.ToUInt32(bytes).TryFormat(destBuffer, out charsWritten),
            NumberType.UInt64 => BitConverter.ToUInt64(bytes).TryFormat(destBuffer, out charsWritten),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null),
        };
    }

    public override bool TryWrite(byte input, char @char, int nibble, out byte output)
    {
        output = default;
        return false;
    }

    public unsafe string? ToString(ReadOnlySpan<byte> buffer, FormatInfo info)
    {
        if (buffer.Length % ByteWidth is not 0)
        {
            return null;
        }

        var sb = new StringBuilder();

        Span<char> tempBuffer = stackalloc char[Width];

        for (var i = 0; i < buffer.Length / ByteWidth; i++)
        {
            var bytes = buffer.Slice(i * ByteWidth, ByteWidth);
            if (TryFormat(bytes, _type, tempBuffer, _reverseBytes, out var charsWritten))
            {
                sb.Append(tempBuffer.Slice(0, charsWritten));
            }
            
            sb.Append(' ');
        }

        return sb.ToString();
    }
}