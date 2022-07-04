using System.Runtime.CompilerServices;

namespace HexControl.SharedControl.Framework.Mapping;

internal interface IPropertyMapper
{
    Task SetValueAsync(string propertyName, object? value);

    Task SetValueAsync<TValue>(TValue? value, [CallerMemberName] string? propertyName = null);

    TResult? GetValueNullable<TResult>(object? _, [CallerMemberName] string? propertyName = null);
    TResult? GetValueNullable<TResult>([CallerMemberName] string? propertyName = null);

    TResult GetValue<TResult>(object? _, [CallerMemberName] string? propertyName = null);
    TResult GetValue<TResult>([CallerMemberName] string? propertyName = null);
}