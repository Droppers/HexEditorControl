using System;
using System.Collections.Generic;
using HexControl.PatternLanguage.Standard;

namespace HexControl.PatternLanguage.Functions;

public class FunctionRegistry
{
    private static readonly Lazy<FunctionRegistry> StandardLazy = new(InitializeStandardRegistry);

    private readonly Dictionary<string, FunctionDefinition> _functions;

    public FunctionRegistry()
    {
        _functions = new Dictionary<string, FunctionDefinition>();
    }

    public static FunctionRegistry Standard => StandardLazy.Value;

    public IReadOnlyDictionary<string, FunctionDefinition> Functions => _functions;

    private static FunctionRegistry InitializeStandardRegistry()
    {
        var registry = new FunctionRegistry();
        registry.Register(new StdNamespace());
        registry.Register(new StdMemNamespace());
        return registry;
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public void Register(IFunctionNamespace ns)
    {
        ns.Register(this);
    }

    public void Register(FunctionNamespace ns, string name, FunctionParameterCount parameterCount, FunctionBody body)
    {
        var fullName = GetFullName(ns, name);
        _functions.Add(fullName, new FunctionDefinition(ns, name, parameterCount, body));
    }

    public void Register(string fullName, FunctionParameterCount parameterCount, FunctionBody body)
    {
        _functions.Add(fullName, new FunctionDefinition(null, fullName, parameterCount, body));
    }

    public void Clear()
    {
        _functions.Clear();
    }

    private static string GetFullName(FunctionNamespace ns, string name) => $"{ns}::{name}";
}