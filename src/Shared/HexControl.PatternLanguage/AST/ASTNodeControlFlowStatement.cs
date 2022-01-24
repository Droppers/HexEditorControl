using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeControlFlowStatement : ASTNode
{
    private readonly ASTNode? _returnValue;

    private readonly ControlFlowStatement _type;

    public ASTNodeControlFlowStatement(ControlFlowStatement type, ASTNode? rvalue)
    {
        _type = type;
        _returnValue = rvalue;
    }

    private ASTNodeControlFlowStatement(ASTNodeControlFlowStatement other) : base(other)
    {
        _type = other._type;
        _returnValue = other._returnValue?.Clone();
    }

    public override ASTNode Clone() => new ASTNodeControlFlowStatement(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        Execute(evaluator);

        return Array.Empty<PatternData>();
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        evaluator.CurrentControlFlowStatement = _type;

        if (_returnValue is null)
        {
            return null;
        }

        var literal = (ASTNodeLiteral)_returnValue.Evaluate(evaluator);

        return literal.Literal;
    }
}