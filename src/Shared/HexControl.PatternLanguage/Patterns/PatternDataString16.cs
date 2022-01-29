using System.Text;
using HexControl.Core.Buffers;

namespace HexControl.PatternLanguage.Patterns;

public class PatternDataString16 : PatternData
{
    public PatternDataString16(long offset, long size, Evaluator evaluator, int color = 0)
        : base(offset, size, evaluator, color) { }

    private PatternDataString16(PatternDataString16 other) : base(other) { }

    public override PatternData Clone() => new PatternDataString16(this);

    public override string GetFormattedName() => "String";

    public override string ToString(BaseBuffer buffer)
    {
        var bytes = new byte[Size];
        //std::string buffer(this->getSize(), 0x00);
        buffer.Read(Offset, bytes);

        // TODO: change the endianess
        //for (auto & c : buffer)
        //    c = hex::changeEndianess(c, 2, this->getEndian());

        //std::erase_if(buffer, [](auto c){
        //    return c == 0x00;
        //});

        return Encoding.UTF8.GetString(bytes);
    }
}