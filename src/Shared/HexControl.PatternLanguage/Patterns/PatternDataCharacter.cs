using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataCharacter : PatternData
{
    public PatternDataCharacter(long offset, Evaluator evaluator, uint color = 0)
        : base(offset, 1, evaluator, color) { }

    private PatternDataCharacter(PatternDataCharacter other) : base(other) { }

    public override PatternData Clone() => new PatternDataCharacter(this);

    public override string GetFormattedName() => "char";

    public override string ToString(BaseBuffer buffer) => ((char)buffer.ReadUByte(Offset)).ToString();
}