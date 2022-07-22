using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

public class DecimalCharacterSet : CharacterSet
{
    private readonly NumberType _type;

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

    public DecimalCharacterSet(NumberType type)
    {
        _type = type;
        Groupable = true;
        Width = GetWidth(type);
        DataWidth = GetSize(type);
    }

    public override int GetCharacters(ReadOnlySpan<byte> bytes, Span<char> destBuffer)
    {
        for (var i = 0; i < Width; i++)
        {
            destBuffer[i] = '1';
        }

        // TODO: VERY TEMPORARY
        var converted = (_type switch
        {
            NumberType.Int16 => BitConverter.ToInt16(bytes).ToString(),
            NumberType.Int32 => BitConverter.ToInt32(bytes).ToString(),
            NumberType.Int64 => BitConverter.ToInt64(bytes).ToString(),
            NumberType.UInt16 => BitConverter.ToUInt16(bytes).ToString(),
            NumberType.UInt32 => BitConverter.ToUInt32(bytes).ToString(),
            NumberType.UInt64 => BitConverter.ToUInt64(bytes).ToString(),
            NumberType.Float => BitConverter.ToSingle(bytes).ToString(CultureInfo.CurrentCulture),
            NumberType.Double => BitConverter.ToDouble(bytes).ToString(CultureInfo.CurrentCulture),
            _ => throw new ArgumentOutOfRangeException()
        });
        var s = converted.PadLeft(Width, ' ');
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            destBuffer[i] = c;
        }

        return Width;
    }

    public override bool TryWrite(byte input, char @char, int nibble, out byte output)
    {
        output = default;
        return false;
    }
}