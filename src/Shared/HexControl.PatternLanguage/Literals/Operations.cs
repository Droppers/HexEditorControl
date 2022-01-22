using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexControl.PatternLanguage.Literals
{
    internal interface IEqualityOperations
    {
        public BoolLiteral Greater(Literal other);
        public BoolLiteral Less(Literal other);
        public BoolLiteral Equal(Literal other);
        public BoolLiteral NotEqual(Literal other);
        public BoolLiteral GreaterOrEqual(Literal other);
        public BoolLiteral LessOrEqual(Literal other);

        public BoolLiteral And(Literal other);
        public BoolLiteral Xor(Literal other);
        public BoolLiteral Or(Literal other);
        public BoolLiteral Not(Literal other);
    }

    internal interface IArithmeticOperations
    {
        public Literal Multiply(Literal other);
        public Literal Divide(Literal other);
        public Literal Add(Literal other);
        public Literal Subtract(Literal other);
        public Literal Modulo(Literal other);
    }

    internal interface IBitwiseOperations {
        public Literal BitShiftLeft(Literal other);
        public Literal BitShiftRight(Literal other);
        public Literal BitAnd(Literal other);
        public Literal BitXor(Literal other);
        public Literal BitOr(Literal other);
        public Literal BitNot(Literal other);
    }
}
