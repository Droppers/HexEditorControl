namespace HexControl.PatternLanguage.Patterns;

public class PatternDataPadding : PatternData
{
    public PatternDataPadding(long offset, long size, Evaluator evaluator, uint color = 0)
        : base(offset, size, evaluator, color) { }

    private PatternDataPadding(PatternDataPadding other) : base(other) { }

    public override PatternData Clone() => new PatternDataPadding(this);

    public override string GetFormattedName() => "";
}