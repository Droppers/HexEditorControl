using System.Collections.Generic;
using HexControl.Core.Buffers.Extensions;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeTypeDecl : AttributableASTNode
{
    public ASTNodeTypeDecl(string name, ASTNode type, Endianess? endian = null)
    {
        Name = name;
        Type = type;
        Endian = endian;
    }

    private ASTNodeTypeDecl(ASTNodeTypeDecl other) : this(other.Name, other.Type, other.Endian) { }
    public override bool MultiPattern => Type.MultiPattern;

    public string Name { get; }
    public ASTNode Type { get; }
    public Endianess? Endian { get; }

    public override ASTNode Clone() => new ASTNodeTypeDecl(this);

    public override ASTNode Evaluate(Evaluator evaluator) => Type.Evaluate(evaluator);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        if (!Type.MultiPattern)
        {
            return new[] {CreatePattern(evaluator)};
        }

        var patterns = Type.CreatePatterns(evaluator);

        for (var i = 0; i < patterns.Count; i++)
        {
            var pattern = patterns[i];
            if (pattern is null)
            {
                continue;
            }

            if (Name.Length > 0)
            {
                pattern.TypeName = Name;
            }

            pattern.Endian = Endian ?? evaluator.DefaultEndian;
        }

        return patterns;
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        var pattern = Type.CreatePattern(evaluator);
        if (pattern is null)
        {
            return null;
        }

        if (Name.Length > 0)
        {
            pattern.TypeName = Name;
        }

        pattern.Endian = Endian ?? evaluator.DefaultEndian;
        return pattern;
    }
}