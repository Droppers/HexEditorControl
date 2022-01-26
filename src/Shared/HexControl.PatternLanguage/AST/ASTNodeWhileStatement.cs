using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeWhileStatement : ASTNode
{
    private readonly IReadOnlyList<ASTNode> _body;

    private readonly ASTNode _condition;
    private readonly ASTNode? _postExpression;

    public ASTNodeWhileStatement(ASTNode condition, IReadOnlyList<ASTNode> body, ASTNode? postExpression = null)
    {
        _condition = condition;
        _body = body;
        _postExpression = postExpression;
    }

    private ASTNodeWhileStatement(ASTNodeWhileStatement other) : base(other)
    {
        _condition = other._condition.Clone();
        _body = other._body.Clone();
        _postExpression = other._postExpression?.Clone();
    }

    public override ASTNode Clone() => new ASTNodeWhileStatement(this);

    public override Literal? Execute(Evaluator evaluator)
    {
        var loopIterations = 0;
        while (EvaluateCondition(evaluator))
        {
            evaluator.HandleAbort();

            var variables = evaluator.PushScope(evaluator.ScopeAt(0).Entries).Entries;

            var ctrlFlow = ControlFlowStatement.None;
            for (var i = 0; i < _body.Count; i++)
            {
                var statement = _body[i];
                var result = statement.Execute(evaluator);

                ctrlFlow = evaluator.CurrentControlFlowStatement;
                evaluator.CurrentControlFlowStatement = ControlFlowStatement.None;
                if (ctrlFlow == ControlFlowStatement.Return)
                {
                    evaluator.PopScope(true);
                    return result;
                }

                if (ctrlFlow != ControlFlowStatement.None)
                {
                    evaluator.PopScope(true);
                    break;
                }
            }

            _postExpression?.Execute(evaluator);

            loopIterations++;
            if (loopIterations >= evaluator.LoopLimit)
            {
                throw new Exception($"loop iterations exceeded limit of {evaluator.LoopLimit}");
            }

            evaluator.PopScope(true);

            if (ctrlFlow == ControlFlowStatement.Break)
            {
                break;
            }

            if (ctrlFlow == ControlFlowStatement.Continue) { }
        }

        return null;
    }

    public bool EvaluateCondition(Evaluator evaluator)
    {
        var literalNode = (ASTNodeLiteral)_condition.Evaluate(evaluator);
        return literalNode.Literal.ToBool();
    }
}