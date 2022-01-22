using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Extensions;
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
        var scope = evaluator.GetScope(0).Entries;
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

        var variables = evaluator.GetScope(0).Entries;
        var startVariableCount = variables.Count;

        evaluator.PushScope(null, variables);

        foreach (var statement in body)
        {
            var result = statement.Execute(evaluator);
            var ctrlStatement = evaluator.GetCurrentControlFlowStatement();

            if (ctrlStatement != ControlFlowStatement.None)
            {
                CleanUpScope(evaluator, startVariableCount, variables.Count);
                return result;
            }
        }

        CleanUpScope(evaluator, startVariableCount, variables.Count);
        return null;
    }

    // TODO: modernize this
    private static void CleanUpScope(Evaluator evaluator, int startVariableCount, int count)
    {
        var variables = evaluator.GetScope(0).Entries;
        var stackSize = evaluator.GetStack().Count;
        for (var i = startVariableCount; i < count; i++)
        {
            variables.RemoveAt(variables.Count - 1);
            stackSize--;
        }

        if (stackSize < 0)
        {
            throw new Exception("stack pointer underflow!");
        }

        evaluator.GetStack().Shrink(stackSize);
        evaluator.PopScope();
    }

    private bool EvaluateCondition(Evaluator evaluator)
    {
        var literalNode = (ASTNodeLiteral)_condition.Evaluate(evaluator);
        return literalNode.Literal.ToBool();
    }
}