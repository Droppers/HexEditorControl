using System.Collections;
using System.Collections.Generic;

namespace HexControl.PatternLanguage.Functions;

public interface IFunctionFactory
{
    public string Namespace { get; }
    public IEnumerable<ContentRegistry.FunctionRegistry.Function> Register();
}