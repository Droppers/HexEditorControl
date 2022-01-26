using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeMultiVariableDecl : ASTNode
{
    private readonly IReadOnlyList<ASTNode> _variables;

    public ASTNodeMultiVariableDecl(IReadOnlyList<ASTNode> variables)
    {
        _variables = variables;
    }

    private ASTNodeMultiVariableDecl(ASTNodeMultiVariableDecl other) : base(other)
    {
        _variables = other._variables.Clone();
    }

    public override ASTNode Clone() => new ASTNodeMultiVariableDecl(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var patterns = new List<PatternData>(_variables.Count);

        for (var i = 0; i < _variables.Count; i++)
        {
            var node = _variables[i];
            var newPatterns = node.CreatePatterns(evaluator);
            patterns.AddRange(newPatterns);
        }

        return patterns;
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        for (var i = 0; i < _variables.Count; i++)
        {
            var variable = _variables[i];
            var variableDecl = (ASTNodeVariableDecl)variable;

            evaluator.CreateVariable(variableDecl.Name, variableDecl.Type.Evaluate(evaluator));
        }

        return null;
    }
}