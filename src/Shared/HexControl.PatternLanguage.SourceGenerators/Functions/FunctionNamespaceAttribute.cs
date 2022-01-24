using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.SourceGenerators.Functions
{
    public class FunctionNamespaceAttribute : Attribute
    {
        public string Namespace { get; }

        public FunctionNamespaceAttribute(string @namespace)
        {
            Namespace = @namespace;
        }
    }
}
