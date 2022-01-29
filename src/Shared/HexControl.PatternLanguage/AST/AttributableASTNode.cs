using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.AST;

internal abstract class AttributableASTNode : ASTNode
{
    private readonly List<ASTNodeAttribute> _attributes;

    protected AttributableASTNode()
    {
        _attributes = new List<ASTNodeAttribute>();
    }

    protected AttributableASTNode(AttributableASTNode other) : base(other)
    {
        _attributes = new List<ASTNodeAttribute>(other.Attributes);
    }

    public IReadOnlyList<ASTNodeAttribute> Attributes => _attributes;

    public void AddAttribute(ASTNodeAttribute attribute)
    {
        _attributes.Add(attribute);
    }
    
    // TODO: refactor this to be normal code
    protected static void ApplyVariableAttributes(Evaluator evaluator, AttributableASTNode attributable,
        PatternData pattern)
    {
        var endOffset = evaluator.CurrentOffset;
        evaluator.CurrentOffset = pattern.Offset;

        for (var i = 0; i < attributable._attributes.Count; i++)
        {
            var attribute = attributable._attributes[i];
            var name = attribute.Attribute;
            var value = attribute.Value;
            
            var requiresValue = () =>
            {
                if (value is null)
                {
                    throw new Exception($"used attribute '{name}' without providing a value"); // LOL
                }

                return true;
            };

            var noValue = () =>
            {
                if (value is not null)
                {
                    throw new Exception($"provided a value to attribute '{name}' which doesn't take one");
                }

                return true;
            };

            if (name == "color" && requiresValue())
            {
                pattern.Color = Convert.ToInt32(value!, 16);
            }
            else if (name == "name" && requiresValue())
            {
                pattern.StaticData.DisplayName = value!;
            }
            else if (name == "comment" && requiresValue())
            {
                pattern.StaticData.Comment = value!;
            }
            else if (name == "hidden" && noValue())
            {
                pattern.Hidden = true;
            }
            else if (name == "inline" && noValue())
            {
                if (pattern is not IPatternInlinable)
                {
                    throw new Exception("inline attribute can only be applied to nested types"); // pass node
                }

                pattern.Inlined = true;
            }
            else if (name == "format" && requiresValue())
            {
                var functions = evaluator.CustomFunctions;

                if (!functions.Functions.TryGetValue(value!, out var function))
                {
                    throw new Exception($"cannot find formatter function '{value}'"); // pass node
                }

                if (function.ParameterCount != FunctionParameterCount.Exactly(1))
                {
                    throw new Exception("formatter function needs exactly one parameter"); // pass node
                }

                pattern.StaticData.FormatterFunction = function.Body;
            }
            else if (name == "transform" && requiresValue())
            {
                var functions = evaluator.CustomFunctions;
                if (!functions.Functions.TryGetValue(value!, out var function))
                {
                    throw new Exception($"cannot find transform function '{value}'"); // pass node
                }

                if (function.ParameterCount != FunctionParameterCount.Exactly(1))
                {
                    throw new Exception("transform function needs exactly one parameter"); // pass node
                }

                pattern.StaticData.TransformFunction = function.Body;
            }
            else if (name == "pointer_base" && requiresValue())
            {
                var functions = evaluator.CustomFunctions;
                if (!functions.Functions.TryGetValue(value!, out var function))
                {
                    throw new Exception($"cannot find pointer base function '{value}'"); // pass node
                }

                if (function.ParameterCount != FunctionParameterCount.Exactly(1))
                {
                    throw new Exception("pointer base function needs exactly one parameter"); // pass node
                }

                if (pattern is PatternDataPointer pointerPattern)
                {
                    var pointerValue = pointerPattern.PointedAtAddress;

                    var result = function.Body(evaluator, new Literal[] {pointerValue});

                    if (result is null)
                    {
                        throw new Exception("pointer base function did not return a value"); // pass node
                    }

                    pointerPattern.PointedAtAddress = result.ToInt64() + pointerValue;
                }
                else
                {
                    throw new Exception("pointer_base attribute may only be applied to a pointer");
                }
            }
        }


        evaluator.CurrentOffset = endOffset;
    }
}