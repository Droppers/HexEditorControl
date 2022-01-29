namespace HexControl.PatternLanguage.Patterns;

public class PatternDataSigned : PatternData
{
    public PatternDataSigned(long offset, long size, Evaluator evaluator, int color = 0)
        : base(offset, size, evaluator, color) { }

    private PatternDataSigned(PatternDataSigned other) : base(other) { }

    public override PatternData Clone() => new PatternDataSigned(this);

    public override string GetFormattedName()
    {
        return Size switch
        {
            1 => "s8",
            2 => "s16",
            4 => "s32",
            8 => "s64",
            16 => "s128",
            _ => "Signed data"
        };
    }
}