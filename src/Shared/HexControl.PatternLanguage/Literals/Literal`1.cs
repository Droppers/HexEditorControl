namespace HexControl.PatternLanguage.Literals;

public abstract class Literal<TValue> : Literal
{
    public Literal(TValue value)
    {
        Value = value;
    }

    public TValue Value { get; }
}