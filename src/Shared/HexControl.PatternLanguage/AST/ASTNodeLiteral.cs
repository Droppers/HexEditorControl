using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeLiteral : ASTNode
{
    public ASTNodeLiteral(Literal literal)
    {
        Literal = literal;
    }
    
    public Literal Literal { get; }

    // No reason to clone a literal, since it is a literal.
    public override ASTNode Clone() => this;
}