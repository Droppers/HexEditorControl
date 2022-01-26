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

        //if (parameterCount == ContentRegistry.FunctionRegistry.UnlimitedParameters)
        //{
        //    // Don't check parameter count
        //}
        //else if ((parameterCount & ContentRegistry.FunctionRegistry.LessParametersThan) != 0)
        //{
        //    if (evaluatedParams.Length >= (parameterCount & ~ContentRegistry.FunctionRegistry.LessParametersThan))
        //    {
        //        //LogConsole.abortEvaluation($"too many parameters for function '{m_functionName}'. Expected {function.ParameterCount & ~ContentRegistry.PatternLanguage.LessParametersThan}", this);
        //        throw new Exception(
        //            $"too many parameters for function '{_functionName}'. Expected {parameterCount & ~ContentRegistry.FunctionRegistry.LessParametersThan}");
        //    }
        //}
        //else if ((parameterCount & ContentRegistry.FunctionRegistry.MoreParametersThan) != 0)
        //{
        //    if (evaluatedParams.Length <= (parameterCount & ~ContentRegistry.FunctionRegistry.MoreParametersThan))
        //    {
        //        //LogConsole.abortEvaluation($"too few parameters for function '{m_functionName}'. Expected {function.ParameterCount & ~ContentRegistry.PatternLanguage.MoreParametersThan}", this);
        //        throw new Exception(
        //            $"too few parameters for function '{_functionName}'. Expected {parameterCount & ~ContentRegistry.FunctionRegistry.MoreParametersThan}");
        //    }
        //}
        //else if (parameterCount != evaluatedParams.Length)
        //{
        //    //LogConsole.abortEvaluation($"invalid number of parameters for function '{m_functionName}'. Expected {function.ParameterCount}", this);
        //    throw new Exception(
        //        $"invalid number of parameters for function '{_functionName}'. Expected {parameterCount}");
        //}

        try
        {
            if (function.Dangerous && evaluator.DangerousFunctionPermission is not DangerousFunctionPermission.Allow)
            {
                evaluator.DangerousFunctionCalled();

                //while (evaluator.DangerousFunctionPermission is DangerousFunctionPermission.Ask)
                //{
                //    evaluator
                //    Thread.Sleep(100); // TODO: actually ask, don't just block lol!
                //}

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