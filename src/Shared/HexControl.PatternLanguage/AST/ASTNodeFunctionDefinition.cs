using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeFunctionDefinition : ASTNode
{
    private readonly IReadOnlyList<ASTNode> _body;

    private readonly string _name;
    private readonly IReadOnlyList<(string, ASTNode)> _params;

    public ASTNodeFunctionDefinition(string name, IReadOnlyList<(string, ASTNode)> @params, IReadOnlyList<ASTNode> body)
    {
        _name = name;
        _params = @params;
        _body = body;
    }

    private ASTNodeFunctionDefinition(ASTNodeFunctionDefinition other) : base(other)
    {
        _name = other._name;
        
        var @params = new List<(string, ASTNode)>(other._params.Count);
        foreach (var (name, type) in other._params)
        {
            @params.Add((name, type.Clone()));
        }

        _params = @params;
        _body = other._body.Clone();
    }

    public override ASTNode Clone() => new ASTNodeFunctionDefinition(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        Literal? Executor(Evaluator ctx, IReadOnlyList<Literal> @params)
        {
            ctx.PushScope(null, new List<PatternData>());

            var paramIndex = 0;
            foreach (var (name, type) in _params)
            {
                ctx.CreateVariable(name, type, @params[paramIndex]);
                ctx.SetVariable(name, @params[paramIndex]);

                paramIndex++;
            }

            foreach (var statement in _body)
            {
                var result = statement.Execute(ctx);

                if (ctx.GetCurrentControlFlowStatement() == ControlFlowStatement.None)
                {
                    continue;
                }

                var willBreak = ctx.GetCurrentControlFlowStatement() switch
                {
                    ControlFlowStatement.Break => throw new Exception("break statement not within a loop"),
                    //ctx.getConsole().abortEvaluation("break statement not within a loop", statement);
                    ControlFlowStatement.Continue => throw new Exception("continue statement not within a loop"),
                    _ => true
                };

                if (willBreak)
                {
                    break;
                }

                ctx.SetCurrentControlFlowStatement(ControlFlowStatement.None);
                ctx.PopScope();
                return result;
            }

            ctx.PopScope();
            return null;
        }

        ;

        evaluator.AddCustomFunction(_name, _params.Count, Executor);

        return null;
    }
}