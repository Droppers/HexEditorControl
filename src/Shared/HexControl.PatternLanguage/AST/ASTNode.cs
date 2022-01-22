using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal abstract class ASTNode : ICloneable<ASTNode>
{
    protected ASTNode() { }

    protected ASTNode(ASTNode node)
    {
        Debug.WriteLine($"Cloned pattern: {node.GetType().Name}");
        LineNumber = node.LineNumber;
    }

    public int LineNumber { get; set; }

    public abstract ASTNode Clone();

    public virtual ASTNode Evaluate(Evaluator evaluator) => Clone();

    public virtual IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator) => Array.Empty<PatternData>().ToList();

    public virtual Literal? Execute(Evaluator evaluator) => throw
        //LogConsole.abortEvaluation("cannot Execute non-function statement", this);
        new Exception("cannot Execute non-function statement");
}