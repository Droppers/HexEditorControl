using System.Runtime.CompilerServices;

namespace HexControl.SharedControl.Framework;

internal abstract class PropertyMapper
{
    public abstract Task SetValue(string propertyName, object? value);

    public TValue GetValue<TValue>(object _, [CallerMemberName] string? propertyName = null)
    {
        var value = GetValueInternal<TValue>(propertyName);
        if (value is null)
        {
            throw new InvalidOperationException("GetValue does not handle nullable properties.");
        }

        return value;
    }

    public TValue? GetValueNullable<TValue>(object? _, [CallerMemberName] string? propertyName = null) =>
        GetValueInternal<TValue>(propertyName);

    protected abstract TValue? GetValueInternal<TValue>(string? propertyName);

    protected TTarget Cast<TTarget>(object value) => (TTarget)value;

    protected static TCast? CastNullable<TCast>(object? obj) => (TCast?)obj;
}