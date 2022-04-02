using System.Text;
using HexControl.Core.Helpers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataString : PatternData
{
    public PatternDataString(long offset, long size, Evaluator evaluator, IntegerColor? color = null)
        : base(offset, size, evaluator, color) { }

    private PatternDataString(PatternDataString other) : base(other) { }

    public override PatternData Clone() => new PatternDataString(this);

    public override string GetFormattedName() => "String";

    public override string ToString(Evaluator evaluator)
    {
        var bytes = new byte[Size];
        //std::string buffer(this->getSize(), 0x00);
        evaluator.Buffer.Read(bytes, Offset);

        //std::erase_if(buffer, [](auto c){
        //    return c == 0x00;
        //});

        return Encoding.ASCII.GetString(bytes);
    }
}