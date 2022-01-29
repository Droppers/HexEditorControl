using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeCompoundStatement : ASTNode
{
    private readonly bool _newScope;

    private readonly IReadOnlyList<ASTNode> _statements;

    public ASTNodeCompoundStatement(IReadOnlyList<ASTNode> statements, bool newScope = false)
    {
        _statements = statements;
        _newScope = newScope;
    }

    private ASTNodeCompoundStatement(ASTNodeCompoundStatement other) : base(other)
    {
        _statements = other._statements.Clone();
    }

    public override ASTNode Clone() => new ASTNodeCompoundStatement(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        ASTNode? result = null;

        for (var i = 0; i < _statements.Count; i++)
        {
            var statement = _statements[i];
            result = statement.Evaluate(evaluator);
        }

        return result;
    }

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        List<PatternData> result = new();

        for (var i = 0; i < _statements.Count; i++)
        {
            var statement = _statements[i];
            if (statement.MultiPattern)
            {
                result.AddRange(statement.CreatePatterns(evaluator));
            }
            else
            {
                result.Add(statement.CreatePattern(evaluator));
            }
        }

        return result;
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        Literal? result = null;

        var variables = evaluator.ScopeAt(0).Entries;

        if (_newScope)
        {
            evaluator.PushScope(variables);
        }

        for (var i = 0; i < _statements.Count; i++)
        {
            var statement = _statements[i];
            result = statement.Execute(evaluator);
            if (evaluator.CurrentControlFlowStatement != ControlFlowStatement.None)
            {
                return result;
            }
        }

        if (_newScope)
        {
            evaluator.PopScope(true);
        }

        return result;
    }
}