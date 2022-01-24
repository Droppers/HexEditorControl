using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexControl.PatternLanguage.SourceGenerators.Helpers;
using Microsoft.CodeAnalysis;

namespace HexControl.PatternLanguage.SourceGenerators.Functions
{
    [Generator]
    internal class StandardFunctionProxyGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            throw new NotImplementedException();
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var types = AttributeHelper.GetTypesWithAttribute<FunctionNamespaceAttribute>();
            foreach (var (type, nsAttribute) in types)
            {
                var name = type.Name;


            }
            throw new NotImplementedException();
        }
    }
}
