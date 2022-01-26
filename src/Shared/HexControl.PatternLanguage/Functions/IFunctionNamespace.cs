using System;

namespace HexControl.PatternLanguage.Functions;

public abstract class IFunctionNamespace
{
    private FunctionRegistry? _registry;
    public abstract FunctionNamespace Namespace { get; }

    public void Register(FunctionRegistry registry)
    {
        _registry = registry;
        RegisterFunctions();
    }

    protected abstract void RegisterFunctions();

    public void Register(string name, FunctionParameterCount parameterCount, FunctionBody body)
    {
        if (_registry is null)
        {
            throw new InvalidOperationException("Add the namespace to a function registry before calling Register().");
        }

        _registry.Register(Namespace, name, parameterCount, body);
    }
}