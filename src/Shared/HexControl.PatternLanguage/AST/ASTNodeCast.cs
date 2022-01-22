using System;
using System.Linq;
using HexControl.PatternLanguage.Literals;

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
        var typePattern = _type.CreatePatterns(evaluator)[0]; // TODO: keep for endian purposes :)

        // TODO: implement endian swapping
        //auto endianAdjustedValue = hex::changeEndianess(value, typePattern->getSize(), typePattern->getEndian())
        var literal = literalNode.Literal;
        Literal castedLiteral = type switch
        {
            Token.ValueType.Unsigned8Bit => (ulong)(byte)literal.ToUnsignedLong(),
            Token.ValueType.Unsigned16Bit => (ulong)(ushort)literal.ToUnsignedLong(),
            Token.ValueType.Unsigned32Bit => (ulong)(uint)literal.ToUnsignedLong(),
            Token.ValueType.Unsigned64Bit => literal.ToUnsignedLong(),
            Token.ValueType.Unsigned128Bit => literal.ToUnsignedLong(), // no 128 bit numbers
            Token.ValueType.Signed8Bit => (long)(byte)literal.ToSignedLong(),
            Token.ValueType.Signed16Bit => (long)(ushort)literal.ToSignedLong(),
            Token.ValueType.Signed32Bit => (long)(uint)literal.ToSignedLong(),
            Token.ValueType.Signed64Bit => literal.ToSignedLong(),
            Token.ValueType.Signed128Bit => literal.ToSignedLong(), // no 128 bit numbers
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