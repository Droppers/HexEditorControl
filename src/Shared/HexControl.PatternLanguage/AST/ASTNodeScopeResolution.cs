using System;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeScopeResolution : ASTNode
{
    private readonly string _name;

    private readonly ASTNode _type;

    public ASTNodeScopeResolution(ASTNode type, string name)
    {
        _type = type;
        _name = name;
    }

    private ASTNodeScopeResolution(ASTNodeScopeResolution other) : base(other)
    {
        _type = other._type.Clone();
        _name = other._name;
    }

    public override ASTNode Clone() => new ASTNodeScopeResolution(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        var type = _type.Evaluate(evaluator);

        if (type is ASTNodeEnum enumType)
        {
            foreach (var (name, value) in enumType.Entries)
            {
                if (name == _name)
                {
                    return value.Evaluate(evaluator);
                }
            }
        }
        else
        {
            throw new Exception("invalid scope resolution. Cannot access this type"); //LogConsole::abortEvaluation
        }

        throw new Exception($"could not find constant '{_name}'"); //LogConsole::abortEvaluation
    }
}