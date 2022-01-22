using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Patterns;

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

        var customFunctions = evaluator.GetCustomFunctions();
        var functions = ContentRegistry.PatternLanguage.GetFunctions();

        // TODO: why is this here
        foreach (var (name, func) in customFunctions)
        {
            functions[name] = func;
        }

        if (!functions.ContainsKey(_functionName))
        {
            throw new Exception($"call to unknown function '{_functionName}'");
            //LogConsole.abortEvaluation($"call to unknown function '{this.m_functionName}'", this);
        }

        var (parameterCount, body, dangerous) = functions[_functionName];
        if (parameterCount == ContentRegistry.PatternLanguage.UnlimitedParameters)
        {
            // Don't check parameter count
        }
        else if ((parameterCount & ContentRegistry.PatternLanguage.LessParametersThan) != 0)
        {
            if (evaluatedParams.Length >= (parameterCount & ~ContentRegistry.PatternLanguage.LessParametersThan))
            {
                //LogConsole.abortEvaluation($"too many parameters for function '{m_functionName}'. Expected {function.ParameterCount & ~ContentRegistry.PatternLanguage.LessParametersThan}", this);
                throw new Exception(
                    $"too many parameters for function '{_functionName}'. Expected {parameterCount & ~ContentRegistry.PatternLanguage.LessParametersThan}");
            }
        }
        else if ((parameterCount & ContentRegistry.PatternLanguage.MoreParametersThan) != 0)
        {
            if (evaluatedParams.Length <= (parameterCount & ~ContentRegistry.PatternLanguage.MoreParametersThan))
            {
                //LogConsole.abortEvaluation($"too few parameters for function '{m_functionName}'. Expected {function.ParameterCount & ~ContentRegistry.PatternLanguage.MoreParametersThan}", this);
                throw new Exception(
                    $"too few parameters for function '{_functionName}'. Expected {parameterCount & ~ContentRegistry.PatternLanguage.MoreParametersThan}");
            }
        }
        else if (parameterCount != evaluatedParams.Length)
        {
            //LogConsole.abortEvaluation($"invalid number of parameters for function '{m_functionName}'. Expected {function.ParameterCount}", this);
            throw new Exception(
                $"invalid number of parameters for function '{_functionName}'. Expected {parameterCount}");
        }

        try
        {
            if (dangerous && evaluator.GetDangerousFunctionPermission() != DangerousFunctionPermission.Allow)
            {
                evaluator.DangerousFunctionCalled();

                while (evaluator.GetDangerousFunctionPermission() == DangerousFunctionPermission.Ask)
                {
                    Thread.Sleep(100); // TODO: actually ask, don't just block lol!
                }

                if (evaluator.GetDangerousFunctionPermission() == DangerousFunctionPermission.Deny)
                {
                    //LogConsole.abortEvaluation($"calling of dangerous function '{this.m_functionName}' is not allowed", this);
                    throw new Exception($"calling of dangerous function '{_functionName}' is not allowed");
                }
            }

            var result = body.Invoke(evaluator, evaluatedParams);

            if (result is not null)
            {
                return new ASTNodeLiteral(result);
            }

            return new ASTNodeMathematicalExpression(null!, null!, Token.Operator.Plus);
        }
        catch (Exception ex)
        {
            //LogConsole.abortEvaluation(ex.ToString(), this);
            throw new Exception("function call failed", ex);
        }

        //return null;
    }

    public override Literal? Execute(Evaluator evaluator)
    {
        Evaluate(evaluator);
        return null;
    }
}