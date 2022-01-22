using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;

namespace HexControl.PatternLanguage.Extensions
{
    internal static class BufferExtensions
    {
        public static long ReadInt64(this BaseBuffer buffer, long offset, int size, Endianess endianess)
        {
            return size switch
            {
                sizeof(byte) => buffer.ReadUByte(offset),
                sizeof(short) => buffer.ReadInt16(offset, endianess),
                sizeof(int) => buffer.ReadInt32(offset, endianess),
                sizeof(long) => buffer.ReadInt64(offset, endianess),
                _ => throw new ArgumentOutOfRangeException($"Cannot read signed integer with size {size}.")
            };
        }

        public static ulong ReadUInt64(this BaseBuffer buffer, long offset, int size, Endianess endianess)
        {
            return size switch
            {
                sizeof(byte) => buffer.ReadUByte(offset),
                sizeof(short) => buffer.ReadUInt16(offset, endianess),
                sizeof(int) => buffer.ReadUInt32(offset, endianess),
                sizeof(long) => buffer.ReadUInt64(offset, endianess),
                _ => throw new ArgumentOutOfRangeException($"Cannot read signed integer with size {size}.")
            };
        }
    }
}
