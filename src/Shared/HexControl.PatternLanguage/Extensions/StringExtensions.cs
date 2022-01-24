using System;
using System.Collections.Generic;

namespace HexControl.PatternLanguage.Extensions;

internal static class StringExtensions
{
    public static string ReverseIt(this string str)
    {
        var array = str.ToCharArray();
        Array.Reverse(array);
        return new string(array);
    }

    public static void Shrink<T>(this List<T> list, int size)
    {
        var cur = list.Count;
        if (size < cur)
        {
            list.RemoveRange(size, cur - size);
        }
    }
}