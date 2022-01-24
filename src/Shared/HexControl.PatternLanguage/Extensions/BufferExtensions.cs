using System;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Numerics;

namespace HexControl.PatternLanguage.Extensions;

internal static class BufferExtensions
{
    public static Int128 ReadInt128(this BaseBuffer buffer, long offset, int size, Endianess endianess)
    {
        return size switch
        {
            sizeof(byte) => buffer.ReadUByte(offset),
            sizeof(short) => buffer.ReadInt16(offset, endianess),
            sizeof(int) => buffer.ReadInt32(offset, endianess),
            sizeof(long) => buffer.ReadInt64(offset, endianess),
            Int128.Size => buffer.ReadInt128(offset, endianess),
            _ => throw new ArgumentOutOfRangeException($"Cannot read signed integer with size {size}.")
        };
    }

    public static UInt128 ReadUInt128(this BaseBuffer buffer, long offset, int size, Endianess endianess)
    {
        return size switch
        {
            sizeof(byte) => buffer.ReadUByte(offset),
            sizeof(short) => buffer.ReadUInt16(offset, endianess),
            sizeof(int) => buffer.ReadUInt32(offset, endianess),
            sizeof(long) => buffer.ReadUInt64(offset, endianess),
            UInt128.Size => buffer.ReadUInt128(offset, endianess),
            _ => throw new ArgumentOutOfRangeException($"Cannot read signed integer with size {size}.")
        };
    }
}