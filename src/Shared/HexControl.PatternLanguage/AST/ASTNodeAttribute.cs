namespace HexControl.PatternLanguage.AST;

internal class ASTNodeAttribute : ASTNode
{
    public ASTNodeAttribute(string attribute, string? value = null)
    {
        Attribute = attribute;
        Value = value;
    }

    private ASTNodeAttribute(ASTNodeAttribute other) : base(other)
    {
        Attribute = other.Attribute;
        Value = other.Value;
    }

    public string Attribute { get; }
    public string? Value { get; }

    public override ASTNode Clone() => new ASTNodeAttribute(this);
}