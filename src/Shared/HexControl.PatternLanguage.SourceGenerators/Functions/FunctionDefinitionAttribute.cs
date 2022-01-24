using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.SourceGenerators.Functions
{
    public class FunctionDefinitionAttribute : Attribute
    {
        public string Name { get; }

        public FunctionDefinitionAttribute(string name)
        {
            Name = name;
        }
    }
}
