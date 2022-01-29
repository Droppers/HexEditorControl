using System;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeCast : ASTNode
{
    private readonly ASTNode _type;

    private readonly ASTNode _value;

    public ASTNodeCast(ASTNode value, ASTNode type)
    {
        _value = value;
        _type = type;
    }

    private ASTNodeCast(ASTNodeCast other) : base(other)
    {
        _value = other._value.Clone();
        _type = other._type.Clone();
    }

    public override ASTNode Clone() => new ASTNodeCast(this);

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        var literalNode = (ASTNodeLiteral)_value.Evaluate(evaluator);
        var type = ((ASTNodeBuiltinType)_type.Evaluate(evaluator)).Type;

        var startOffset = evaluator.CurrentOffset;
        var typePattern = _type.CreatePattern(evaluator); // TODO: keep for endian purposes :)

        // TODO: implement endian swapping
        //auto endianAdjustedValue = hex::changeEndianess(value, typePattern->getSize(), typePattern->getEndian())
        var literal = literalNode.Literal;
        Literal castedLiteral = type switch
        {
            Token.ValueType.Unsigned8Bit => (UInt128)(byte)literal.ToUInt128(),
            Token.ValueType.Unsigned16Bit => (UInt128)(ushort)literal.ToUInt128(),
            Token.ValueType.Unsigned32Bit => (UInt128)(uint)literal.ToUInt128(),
            Token.ValueType.Unsigned64Bit => (UInt128)(long)literal.ToUInt128(),
            Token.ValueType.Unsigned128Bit => literal.ToUInt128(),
            Token.ValueType.Signed8Bit => (Int128)(byte)literal.ToInt128(),
            Token.ValueType.Signed16Bit => (Int128)(ushort)literal.ToInt128(),
            Token.ValueType.Signed32Bit => (Int128)(uint)literal.ToInt128(),
            Token.ValueType.Signed64Bit => (Int128)(long)literal.ToInt128(),
            Token.ValueType.Signed128Bit => literal.ToInt128(),
            Token.ValueType.Float => (float)literal.ToDouble(),
            Token.ValueType.Double => literal.ToDouble(),
            Token.ValueType.Character => literal.ToChar(),
            Token.ValueType.Character16 => literal.ToChar16(),
            Token.ValueType.Boolean => literal.ToBool(),
            Token.ValueType.String => literal.ToString()!,
            _ => throw new ArgumentOutOfRangeException()
        };

        evaluator.CurrentOffset = startOffset;

        return new ASTNodeLiteral(castedLiteral);
        //return literal.getValue() switch
        //{
        //    PatternData data => throw new Exception($"cannot cast custom type '{data.getTypeName()}' to '{Token.getTypeName(type)}'"),
        //    string _ => throw new Exception($"cannot cast string to '{Token.getTypeName(type)}'"),
        //    object value => CastPrimitive(type, value, typePattern.getEndian())
        //};
    }
}