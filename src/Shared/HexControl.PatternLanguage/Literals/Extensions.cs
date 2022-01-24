namespace HexControl.PatternLanguage.Literals;

internal static class Extensions
{
    public static UInt128Literal Create(this ulong literal) => new(literal);
}