using System;
using HexControl.Core.Numerics;
using HexControl.PatternLanguage.Helpers;
using HexControl.PatternLanguage.Patterns;

namespace HexControl.PatternLanguage.Literals;

public abstract class Literal
{
    public abstract UInt128 ToUInt128();
    public abstract Int128 ToInt128();

    public abstract long ToInt64();
    public abstract ulong ToUInt64();

    public abstract double ToDouble();
    public abstract bool ToBool();
    public abstract AsciiChar ToChar();
    public abstract char ToChar16();


    public virtual string ToString(bool cast)
    {
        if (!cast)
        {
            throw new Exception($"Literal {GetType().Name} is not a string");
        }

        return ToString()!;
    }

    public static Int128Literal Create(Int128 value) => new(value);

    public static UInt128Literal Create(UInt128 value) => new(value);

    public static DoubleLiteral Create(double value) => new(value);

    public static BoolLiteral Create(bool value) => new(value);

    public static StringLiteral Create(string value) => new(value);

    public static PatternDataLiteral Create(PatternData value) => new(value);

    public static CharLiteral Create(AsciiChar value) => new(value);

    public static Char16Literal Create(char value) => new(value);

    public static implicit operator Literal(Int128 value) => Create(value);
    public static implicit operator Literal(UInt128 value) => Create(value);
    public static implicit operator Literal(double value) => Create(value);
    public static implicit operator Literal(string value) => Create(value);
    public static implicit operator Literal(bool value) => Create(value);
    public static implicit operator Literal(PatternData value) => Create(value);
    public static implicit operator Literal(AsciiChar value) => Create(value);
    public static implicit operator Literal(char value) => Create(value);
}