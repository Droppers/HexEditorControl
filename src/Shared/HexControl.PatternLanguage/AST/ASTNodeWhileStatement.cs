using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Extensions;
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

            var variables = evaluator.GetScope(0).Entries;
            var startVariableCount = variables.Count;
            //ON_SCOPE_EXIT {
            //    s64 stackSize = evaluator->getStack().size();
            //    for (u32 i = startVariableCount; i < variables.size(); i++)
            //    {
            //        stackSize--;
            //        delete variables[i];
            //    }
            //    if (stackSize < 0) LogConsole::abortEvaluation("stack pointer underflow!", this);
            //    evaluator->getStack().resize(stackSize);
            //};

            evaluator.PushScope(null, variables);
            //ON_SCOPE_EXIT { evaluator->popScope(); };

            var ctrlFlow = ControlFlowStatement.None;
            foreach (var statement in _body)
            {
                var result = statement.Execute(evaluator);

                ctrlFlow = evaluator.GetCurrentControlFlowStatement();
                evaluator.SetCurrentControlFlowStatement(ControlFlowStatement.None);
                if (ctrlFlow == ControlFlowStatement.Return)
                {
                    CleanUpScope(evaluator, startVariableCount, variables.Count);
                    return result;
                }

                if (ctrlFlow != ControlFlowStatement.None)
                {
                    CleanUpScope(evaluator, startVariableCount, variables.Count);
                    break;
                }
            }

            if (_postExpression is not null)
            {
                _postExpression.Execute(evaluator);
            }

            loopIterations++;
            if (loopIterations >= evaluator.GetLoopLimit())
            {
                throw new Exception($"loop iterations exceeded limit of {evaluator.GetLoopLimit()}");
            }

            evaluator.HandleAbort();

            CleanUpScope(evaluator, startVariableCount, variables.Count);

            if (ctrlFlow == ControlFlowStatement.Break)
            {
                break;
            }

            if (ctrlFlow == ControlFlowStatement.Continue) { }
        }

        return null;
    }

    // TODO: abit bad
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

    public bool EvaluateCondition(Evaluator evaluator)
    {
        var literalNode = (ASTNodeLiteral)_condition.Evaluate(evaluator);
        return literalNode.Literal.ToBool();
    }
}