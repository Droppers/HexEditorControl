using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeBuiltinType : ASTNode
{
    public ASTNodeBuiltinType(Token.ValueType type)
    {
        Type = type;
    }

    private ASTNodeBuiltinType(ASTNodeBuiltinType node) : base(node)
    {
        Type = node.Type;
    }

    public Token.ValueType Type { get; }

    public override ASTNode Clone() => new ASTNodeBuiltinType(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        var offset = evaluator.CurrentOffset;
        var size = Token.GetTypeSize(Type);

        evaluator.CurrentOffset += size;

        PatternData pattern;
        if (Token.IsUnsigned(Type))
        {
            pattern = new PatternDataUnsigned(offset, size, evaluator);
        }
        else if (Token.IsSigned(Type))
        {
            pattern = new PatternDataSigned(offset, size, evaluator);
        }
        else if (Token.IsFloatingPoint(Type))
        {
            pattern = new PatternDataFloat(offset, size, evaluator);
        }
        else if (Type == Token.ValueType.Boolean)
        {
            pattern = new PatternDataBoolean(offset, evaluator);
        }
        else if (Type == Token.ValueType.Character)
        {
            pattern = new PatternDataCharacter(offset, evaluator);
        }
        else if (Type == Token.ValueType.Character16)
        {
            pattern = new PatternDataCharacter16(offset, evaluator);
        }
        else if (Type == Token.ValueType.Padding)
        {
            pattern = new PatternDataPadding(offset, 1, evaluator);
        }
        else if (Type == Token.ValueType.String)
        {
            pattern = new PatternDataString(offset, 1, evaluator);
        }
        else if (Type == Token.ValueType.Auto)
        {
            return Array.Empty<PatternData>();
        }
        else
        {
            //LogConsole.abortEvaluation("invalid built-in type", this);
            throw new Exception("invalid built-in type");
        }

        if (pattern is null)
        {
            //LogConsole.abortEvaluation("invalid built-in type", this);
            throw new Exception("invalid built-in type");
        }

        pattern.TypeName = Token.GetTypeName(Type);

        return new[] {pattern};
    }
}