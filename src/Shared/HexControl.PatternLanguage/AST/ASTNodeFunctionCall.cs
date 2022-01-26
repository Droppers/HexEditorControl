using System;
using System.Collections.Generic;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage.AST;

internal class ASTNodeFunctionCall : ASTNode
{
    private readonly string _functionName;
    private readonly IReadOnlyList<ASTNode> _params;

    public ASTNodeFunctionCall(string functionName, IReadOnlyList<ASTNode> @params)
    {
        _functionName = functionName;
        _params = @params;
    }

    private ASTNodeFunctionCall(ASTNodeFunctionCall other) : base(other)
    {
        _functionName = other._functionName;
        _params = other._params.Clone();
    }

    public override ASTNode Clone() => new ASTNodeFunctionCall(this);

    public override IReadOnlyList<PatternData> CreatePatterns(Evaluator evaluator)
    {
        Execute(evaluator);

        return Array.Empty<PatternData>();
    }

    public override ASTNode Evaluate(Evaluator evaluator)
    {
        var evaluatedParams = new Literal[_params.Count];
        for (var index = 0; index < _params.Count; index++)
        {
            var param = _params[index];
            var expression = param.Evaluate(evaluator);

            var literal = (ASTNodeLiteral)expression.Evaluate(evaluator);
            evaluatedParams[index] = literal.Literal;
        }

        FunctionDefinition? standardFunction = null;
        if (!evaluator.CustomFunctions.Functions.TryGetValue(_functionName, out var customFunction) &&
            !FunctionRegistry.Standard.Functions.TryGetValue(_functionName, out standardFunction))
        {
            throw new Exception($"call to unknown function '{_functionName}'");
        }

        var function = (customFunction ?? standardFunction)!;
        try
        {
            function.ParameterCount.ThrowIfInvalidParameterCount(evaluatedParams.Length);
        }
        catch (Exception e)
        {
            throw new Exception($"Invalid parameter count for function '{_functionName}': {e.Message}");
        }

        try
        {
            if (function.Dangerous && evaluator.DangerousFunctionPermission is not DangerousFunctionPermission.Allow)
            {
                evaluator.DangerousFunctionCalled();
                
                if (evaluator.DangerousFunctionPermission is DangerousFunctionPermission.Deny)
                {
                    //LogConsole.abortEvaluation($"calling of dangerous function '{this.m_functionName}' is not allowed", this);
                    throw new Exception($"calling of dangerous function '{_functionName}' is not allowed");
                }
            }

            var result = function.Body.Invoke(evaluator, evaluatedParams);

            if (result is not null)
            {
                return new ASTNodeLiteral(result);
            }

            return new ASTNodeMathematicalExpression(null!, null!, Token.Operator.Plus);
        }
        catch (Exception ex)
        {
            throw new Exception("function call failed", ex);
        }
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        Evaluate(evaluator);
        return null;
    }
}