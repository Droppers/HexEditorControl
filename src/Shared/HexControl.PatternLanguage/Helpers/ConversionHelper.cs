using System;

namespace HexControl.PatternLanguage.Helpers;

internal class ConversionHelper
{
    public static int ParseIntAgnostic(string value)
    {
        if (value.Length > 1 && value[1] is 'x' or 'X')
        {
            return Convert.ToInt32(value, 16);
        }

        return int.Parse(value);
    }
}