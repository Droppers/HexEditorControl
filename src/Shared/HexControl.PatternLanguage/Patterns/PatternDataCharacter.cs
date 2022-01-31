using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataCharacter : PatternData
{
    public PatternDataCharacter(long offset, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, 1, evaluator, color) { }

    private PatternDataCharacter(PatternDataCharacter other) : base(other) { }

    public override PatternData Clone() => new PatternDataCharacter(this);

    public override string GetFormattedName() => "char";

    public override string ToString(Evaluator evaluator) => ((char)evaluator.Buffer.ReadUByte(Offset)).ToString();
}