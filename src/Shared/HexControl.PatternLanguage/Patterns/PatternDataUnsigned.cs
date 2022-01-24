namespace HexControl.PatternLanguage.Patterns;

public class PatternDataUnsigned : PatternData
{
    public PatternDataUnsigned(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color) { }

    private PatternDataUnsigned(PatternDataUnsigned other) : base(other) { }

    public override PatternData Clone() => new PatternDataUnsigned(this);

    public override string GetFormattedName()
    {
        return Size switch
        {
            1 => "u8",
            2 => "u16",
            4 => "u32",
            8 => "u64",
            16 => "u128",
            _ => "Unsigned data"
        };
    }
}