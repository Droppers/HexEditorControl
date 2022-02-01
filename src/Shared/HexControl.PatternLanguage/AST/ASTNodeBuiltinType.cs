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

    private ASTNodeBuiltinType(ASTNodeBuiltinType other) : base(other)
    {
        Type = other.Type;
    }

    public override bool MultiPattern => false;

    public Token.ValueType Type { get; }

    public override ASTNode Clone() => new ASTNodeBuiltinType(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        var offset = evaluator.CurrentOffset;
        var size = Token.GetTypeSize(Type);

        evaluator.CurrentOffset += size;

        PatternData pattern;
        if (Type == Token.ValueType.VariableLengthQuantity)
        {
            pattern = new PatternDataUnsigned(offset, 1, evaluator);
        }
        else if (Token.IsUnsigned(Type))
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
            return null;
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

        pattern.StaticData = StaticData;
        pattern.TypeName = Token.GetTypeName(Type);
        return pattern;
    }
}