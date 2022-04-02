using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodePointerVariableDecl : AttributableASTNode
{
    private readonly string _name;
    private readonly ASTNode? _placementOffset;
    private readonly ASTNodeTypeDecl _sizeType;
    private readonly ASTNodeTypeDecl _type;

    public ASTNodePointerVariableDecl(string name, ASTNodeTypeDecl type, ASTNodeTypeDecl sizeType, ASTNode? placementOffset = null)
    {
        _name = name;
        _type = type;
        _sizeType = sizeType;
        _placementOffset = placementOffset;
    }

    private ASTNodePointerVariableDecl(ASTNodePointerVariableDecl other) : base(other)
    {
        _name = other._name;
        _type = other._type;
        _sizeType = other._sizeType;

        _placementOffset = other._placementOffset?.Clone();
    }

    public override bool MultiPattern => false;

    public override ASTNode Clone() => new ASTNodePointerVariableDecl(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        return new[] {CreatePattern(evaluator)};
    }

    public override PatternData CreatePattern(Evaluator evaluator)
    {
        if (_placementOffset is not null)
        {
            var offsetNode = (ASTNodeLiteral)_placementOffset.Evaluate(evaluator);

            evaluator.CurrentOffset = offsetNode.Literal switch
            {
                StringLiteral => throw new Exception("placement offset cannot be a string"), // this
                PatternDataLiteral => throw new Exception("placement offset cannot be a custom type"), // this
                _ => offsetNode.Literal.ToInt64()
            };
        }

        var startOffset = evaluator.CurrentOffset;
        var sizePattern = _sizeType.CreatePattern(evaluator);
        var endOffset = evaluator.CurrentOffset;

        var size = sizePattern.Size;
        var pointerAddress = evaluator.Buffer
            .ReadInt128(startOffset, (int)size, sizePattern.Endian ?? evaluator.DefaultEndian);

        var pattern = new PatternDataPointer(startOffset, size, evaluator)
        {
            PointedAtAddress = (long)pointerAddress,
            VariableName = _name,
            Endian = sizePattern.Endian,
            StaticData = StaticData
        };

        evaluator.CurrentOffset = startOffset;
        ApplyVariableAttributes(evaluator, this, pattern);

        evaluator.CurrentOffset = pattern.PointedAtAddress;
        pattern.PointedAtPattern = _type.CreatePattern(evaluator);
        evaluator.CurrentOffset = endOffset;
        return pattern;
    }
}