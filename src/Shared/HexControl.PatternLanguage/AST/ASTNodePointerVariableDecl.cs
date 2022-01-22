using System;
using System.Collections.Generic;
using System.Linq;
using HexControl.PatternLanguage.Extensions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodePointerVariableDecl : AttributableASTNode
{
    private readonly string _name;
    private readonly ASTNode? _placementOffset;
    private readonly ASTNode _sizeType;
    private readonly ASTNode _type;

    public ASTNodePointerVariableDecl(string name, ASTNode type, ASTNode sizeType, ASTNode? placementOffset = null)
    {
        _name = name;
        _type = type;
        _sizeType = sizeType;
        _placementOffset = placementOffset;
    }

    private ASTNodePointerVariableDecl(ASTNodePointerVariableDecl other) : base(other)
    {
        _name = other._name;
        _type = other._type.Clone();
        _sizeType = other._sizeType.Clone();

        _placementOffset = other._placementOffset?.Clone();
    }

    public override ASTNode Clone() => new ASTNodePointerVariableDecl(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        if (_placementOffset is not null)
        {
            var offsetNode = (ASTNodeLiteral)_placementOffset.Evaluate(evaluator);

            evaluator.CurrentOffset = offsetNode.Literal switch
            {
                StringLiteral => throw new Exception("placement offset cannot be a string"), // this
                PatternDataLiteral => throw new Exception("placement offset cannot be a custom type"), // this
                _ => offsetNode.Literal.ToSignedLong()
            };
        }

        var startOffset = evaluator.CurrentOffset;
        var sizePattern = _sizeType.CreatePatterns(evaluator)[0];
        var endOffset = evaluator.CurrentOffset;
        
        var size = sizePattern.Size;
        var pointerAddress = evaluator.GetBuffer()
            .ReadInt64(startOffset, (int)size, sizePattern.Endian);
        
        var pattern = new PatternDataPointer(startOffset, size, evaluator)
        {
            PointedAtAddress = pointerAddress,
            VariableName = _name,
            Endian = sizePattern.Endian
        };

        evaluator.CurrentOffset = startOffset;
        ApplyVariableAttributes(evaluator, this, pattern);

        evaluator.CurrentOffset = pattern.PointedAtAddress;
        pattern.PointedAtPattern = _type.CreatePatterns(evaluator)[0];
        evaluator.CurrentOffset = endOffset;

        return new [] {pattern};
    }
}