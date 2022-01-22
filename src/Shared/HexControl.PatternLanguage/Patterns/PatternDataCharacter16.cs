using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataCharacter16 : PatternData
{
    public PatternDataCharacter16(long offset, Evaluator evaluator, uint color = 0)
        : base(offset, 2, evaluator, color) { }

    private PatternDataCharacter16(PatternDataCharacter16 other) : base(other) { }

    public override PatternData Clone()
    {
        return new PatternDataCharacter16(this);
    }
        
    public override string GetFormattedName()
    {
        return "char16";
    }

    public override string ToString(BaseBuffer buffer)
    {
        return buffer.ReadChar(Offset, Endian).ToString();
    }
}