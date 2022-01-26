using System;

namespace HexControl.PatternLanguage.Functions;

public readonly struct FunctionParameterCount : IEquatable<FunctionParameterCount>
{
    private readonly RestrictionType _type;
    private readonly int _count;

    private enum RestrictionType
    {
        Unlimited,
        None,
        Exactly,
        AtLeast,
        AtMost
    }

    private FunctionParameterCount(RestrictionType type, int count = -1)
    {
        _type = type;
        _count = count;
    }

    public static readonly FunctionParameterCount Unlimited = new(RestrictionType.Unlimited);
    public static readonly FunctionParameterCount None = new(RestrictionType.None);

    public static FunctionParameterCount AtLeast(int count) => new(RestrictionType.AtLeast, count);

    public static FunctionParameterCount AtMost(int count) => new(RestrictionType.AtMost, count);

    public static FunctionParameterCount Exactly(int count) => new(RestrictionType.Exactly, count);

    internal bool ThrowIfInvalidParameterCount(int count)
    {
        return _type switch
        {
            RestrictionType.None when count > 0 => throw new ArgumentOutOfRangeException(
                $"Expected zero parameters but got {count} parameters instead."),
            RestrictionType.Exactly when count != _count => throw new ArgumentOutOfRangeException(
                $"Expected exactly {_count} parameters but got {count} parameters instead."),
            RestrictionType.AtLeast when count < _count => throw new ArgumentOutOfRangeException(
                $"Expected at least {_count} parameters but got {count} parameters instead."),
            RestrictionType.AtMost when count > _count => throw new ArgumentOutOfRangeException(
                $"Expected at most {_count} parameters but got {count} parameters instead."),
            _ => true
        };
    }

    public override bool Equals(object? obj) => obj is FunctionParameterCount other && Equals(other);

    public bool Equals(FunctionParameterCount other) => _type == other._type && _count == other._count;

    public override int GetHashCode() => HashCode.Combine((int)_type, _count);

    public static bool operator ==(FunctionParameterCount left, FunctionParameterCount right) => left.Equals(right);

    public static bool operator !=(FunctionParameterCount left, FunctionParameterCount right) => !left.Equals(right);


    public static implicit operator FunctionParameterCount(int count) => Exactly(count);
}