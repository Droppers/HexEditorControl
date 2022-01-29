using System.Text;
using HexControl.Core.Buffers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataString : PatternData
{
    public PatternDataString(long offset, long size, Evaluator evaluator, int color = 0)
        : base(offset, size, evaluator, color) { }

    private PatternDataString(PatternDataString other) : base(other) { }

    public override PatternData Clone() => new PatternDataString(this);

    public override string GetFormattedName() => "String";

    public override string ToString(BaseBuffer buffer)
    {
        var bytes = new byte[Size];
        //std::string buffer(this->getSize(), 0x00);
        buffer.Read(Offset, bytes);

        //std::erase_if(buffer, [](auto c){
        //    return c == 0x00;
        //});

        return Encoding.ASCII.GetString(bytes);
    }
}