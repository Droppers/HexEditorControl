using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeLiteral : ASTNode
{
    public ASTNodeLiteral(Literal literal)
    {
        Literal = literal;
    }

    //private ASTNodeLiteral(ASTNodeLiteral node) : base(node)
    //{
    //    Literal = node.Literal;
    //}

    public Literal Literal { get; }

    // No reason to clone a literal, since it is a literal.
    public override ASTNode Clone() => this;
}