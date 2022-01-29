namespace HexControl.PatternLanguage.Patterns;

public class PatternDataBoolean : PatternData
{
    public PatternDataBoolean(long offset, Evaluator evaluator, int color = 0)
        : base(offset, 1, evaluator, color) { }

    private PatternDataBoolean(PatternDataBoolean other) : base(other) { }

    public override PatternData Clone() => new PatternDataBoolean(this);

    public override string GetFormattedName() => "bool";
}