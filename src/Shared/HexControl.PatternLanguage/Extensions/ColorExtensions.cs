﻿namespace HexControl.PatternLanguage.Extensions;

internal static class ColorExtensions
{
    public static int FromRgb(int r, int g, int b) => ((r & 0x0ff) << 16) | ((g & 0x0ff) << 8) | (b & 0x0ff);

    public static (byte r, byte g, byte b) ToRgb(this int value)
    {

        var red = (value >> 16) & 255;
        var green = (value >> 8) & 255;
        var blue = (value >> 0) & 255;
        return ((byte)red, (byte)green, (byte)blue);
    }
}