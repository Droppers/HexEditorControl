using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.Literals
{
    public abstract class Literal<TValue> : Literal
    {
        public Literal(TValue value)
        {
            Value = value;
        }

        public TValue Value { get; }
    }
}
