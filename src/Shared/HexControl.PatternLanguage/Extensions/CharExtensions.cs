namespace HexControl.PatternLanguage.Extensions;

internal static class CharExtensions
{
    public static bool IsAlphaNumeric(this char c) => c is <= 'Z' and >= 'A' or <= 'z' and >= 'a' or >= '0' and <= '9';

    public static bool IsAlpha(this char c) => c is <= 'Z' and >= 'A' or <= 'z' and >= 'a';


    public static bool IsNumeric(this char c) => c is <= '9' and >= '0';
}