using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeConditionalStatement : ASTNode
{
    private readonly ASTNode _condition;
    private readonly IReadOnlyList<ASTNode> _falseBody;
    private readonly IReadOnlyList<ASTNode> _trueBody;

    public ASTNodeConditionalStatement(ASTNode condition, IReadOnlyList<ASTNode> trueBody,
        IReadOnlyList<ASTNode> falseBody)
    {
        _condition = condition;
        _trueBody = trueBody;
        _falseBody = falseBody;
    }


    private ASTNodeConditionalStatement(ASTNodeConditionalStatement other) : base(other)
    {
        _condition = other._condition.Clone();
        _trueBody = other._trueBody.Clone();
        _falseBody = other._falseBody.Clone();
    }

    public override ASTNode Clone() => new ASTNodeConditionalStatement(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var scope = evaluator.ScopeAt(0).Entries;
        var body = EvaluateCondition(evaluator) ? _trueBody : _falseBody;

        foreach (var node in body)
        {
            var newPatterns = node.CreatePatterns(evaluator);
            foreach (var pattern in newPatterns)
            {
                scope.Add(pattern.Clone());
            }
        }

        return Array.Empty<PatternData>();
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        var body = EvaluateCondition(evaluator) ? _trueBody : _falseBody;

        var variables = evaluator.ScopeAt(0).Entries;
        evaluator.PushScope(variables);

        foreach (var statement in body)
        {
            var result = statement.Execute(evaluator);
            var ctrlStatement = evaluator.CurrentControlFlowStatement;

            if (ctrlStatement != ControlFlowStatement.None)
            {
                evaluator.PopScope(true);
                return result;
            }
        }

        evaluator.PopScope(true);
        return null;
    }

    private bool EvaluateCondition(Evaluator evaluator)
    {
        var literalNode = (ASTNodeLiteral)_condition.Evaluate(evaluator);
        return literalNode.Literal.ToBool();
    }
}