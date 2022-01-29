using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal abstract class ASTNode : ICloneable<ASTNode>
{
    private PatternData.StaticPatternData? _staticData;
    protected ASTNode() { }

    protected ASTNode(ASTNode other)
    {
        LineNumber = other.LineNumber;
        _staticData = other._staticData;
    }

    protected PatternData.StaticPatternData StaticData => _staticData ??= new PatternData.StaticPatternData();

    public virtual bool MultiPattern => true;

    public int LineNumber { get; set; }

    public abstract ASTNode Clone();

    public virtual ASTNode Evaluate(Evaluator evaluator) => Clone();

    public virtual IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator) =>
        Array.Empty<PatternData>();

    public virtual PatternData CreatePattern(Evaluator evaluator) => CreatePatterns(evaluator)[0];

    public virtual Literal? Execute(Evaluator evaluator) => throw
        //LogConsole.abortEvaluation("cannot Execute non-function statement", this);
        new Exception("cannot Execute non-function statement");
}