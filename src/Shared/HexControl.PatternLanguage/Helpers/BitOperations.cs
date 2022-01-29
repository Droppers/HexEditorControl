using System.Runtime.CompilerServices;

namespace HexControl.PatternLanguage.Helpers;

internal static class BitOperations
{
    private const int LONG_LAST_BYTE_POS = 7;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte GetSmallValue(this long number) => (byte)(number >> (8 * LONG_LAST_BYTE_POS));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetLargeValue(this long number) => number & ~(long.MaxValue << (8 * LONG_LAST_BYTE_POS));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long StoreSmallValue(this long number, byte value) =>
        GetLargeValue(number) | ((long)value << (8 * LONG_LAST_BYTE_POS));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long StoreLargeValue(this long number, long value) =>
        StoreSmallValue(value, (byte)(number >> (8 * LONG_LAST_BYTE_POS)));
}