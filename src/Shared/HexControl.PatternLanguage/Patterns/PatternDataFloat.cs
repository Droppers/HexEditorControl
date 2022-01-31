using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataFloat : PatternData
{
    public PatternDataFloat(long offset, long size, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, size, evaluator, color) { }

    private PatternDataFloat(PatternDataFloat other) : base(other) { }

    public override PatternData Clone() => new PatternDataFloat(this);

    public override string GetFormattedName()
    {
        return Size switch
        {
            4 => "float",
            8 => "double",
            _ => "Floating point data"
        };
    }
}