using System.Runtime.CompilerServices;
using HexControl.Framework.Host;
using JetBrains.Annotations;

namespace HexControl.Framework.Mapping;

internal delegate object? ValueConverter(object? value);

internal class PropertyMapper<TControl> : IPropertyMapper
    where TControl : class
{
    private readonly INativeFactory _factory;

    private readonly Dictionary<string, PropertyAccessor> _propertyAccessors;

    [PublicAPI] protected readonly TControl control;

    protected PropertyMapper(TControl control, INativeFactory factory)
    {
        this.control = control;
        _factory = factory;
        _propertyAccessors = new Dictionary<string, PropertyAccessor>();
    }

    public async Task SetValueAsync(string propertyName, object? value) => await SetValueAsync(value, propertyName);

    public async Task SetValueAsync<TValue>(TValue? value, [CallerMemberName] string? propertyName = null)
    {
        if (!_propertyAccessors.TryGetValue(propertyName!, out var accessor))
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not mapped.");
        }

        object? val = value;
        if (accessor.ToShared is not null)
        {
            val = accessor.ToShared(value);
        }

        await accessor.Setter(val);
    }

    public TResult? GetValueNullable<TResult>(object? _, [CallerMemberName] string? propertyName = null)
        => GetValueNullable<TResult>(propertyName);

    public TResult? GetValueNullable<TResult>([CallerMemberName] string? propertyName = null)
    {
        if (!_propertyAccessors.TryGetValue(propertyName!, out var accessor))
        {
            throw new InvalidOperationException($"Property '{propertyName}' is not mapped.");
        }

        var value = accessor.Getter();
        if (accessor.ToNative is not null)
        {
            value = accessor.ToNative(value);
        }

        return value is TResult cast ? cast : default;
    }

    public TResult GetValue<TResult>(object? _, [CallerMemberName] string? propertyName = null)
        => GetValue<TResult>(propertyName);

    public TResult GetValue<TResult>([CallerMemberName] string? propertyName = null)
    {
        var value = GetValueNullable<TResult>(propertyName);
        if (value is null)
        {
            throw new InvalidOperationException($"Unexpected null value for property '{propertyName}'.");
        }

        return value;
    }

    // TODO: throw ArgumentNullException?
    [PublicAPI]
    protected void Add<TShared>(string propertyName, Action<TShared> setter, Func<TShared> getter, MappingType type)
        => AddNullable(propertyName, value => setter(value!), getter, type);

    // TODO: throw ArgumentNullException?
    [PublicAPI]
    protected void Add<TShared>(string propertyName, Action<TShared> setter, Func<TShared> getter,
        ValueConverter? toShared = null, ValueConverter? toNative = null)
        => AddNullable(propertyName, value => setter(value!), getter, toShared, toNative);

    [PublicAPI]
    protected void AddNullable<TShared>(string propertyName, Action<TShared?> setter, Func<TShared?> getter,
        MappingType type)
    {
        var (toShared, toNative) = GetMapper(type);
        AddNullable(propertyName, setter, getter, toShared, toNative);
    }

    [PublicAPI]
    protected void AddNullable<TShared>(string propertyName, Action<TShared?> setter, Func<TShared?> getter,
        ValueConverter? toShared = null, ValueConverter? toNative = null)
    {
        AddAsyncNullable(
            propertyName,
            value =>
            {
                setter(value ?? default);
                return Task.CompletedTask;
            },
            getter,
            toShared,
            toNative);
    }

    [PublicAPI]
    protected void AddAsync<TShared>(string propertyName, Func<TShared, Task> setter, Func<TShared> getter,
        ValueConverter? toShared = null, ValueConverter? toNative = null)
        => AddAsyncNullable(propertyName, value => setter(value!), getter, toShared, toNative);

    [PublicAPI]
    protected void AddAsyncNullable<TShared>(string propertyName, Func<TShared?, Task> setter, Func<TShared?> getter,
        ValueConverter? toShared = null, ValueConverter? toNative = null)
    {
        var accessor =
            new PropertyAccessor(value => setter(value is TShared cast ? cast : default),
                () => getter(),
                toShared,
                toNative);
        if (!_propertyAccessors.TryAdd(propertyName, accessor))
        {
            throw new InvalidOperationException($"Property '{propertyName}' is already mapped.");
        }
    }

    private (ValueConverter ToShared, ValueConverter ToNative) GetMapper(MappingType type)
    {
        return type switch
        {
            MappingType.Brush => GetBrushMapper(),
            MappingType.Pen => GetPenMapper(),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private (ValueConverter ToShared, ValueConverter ToNative) GetBrushMapper()
    {
        return (
            value => value is null ? null : _factory.WrapBrush(value),
            value => value is null ? null : _factory.UnwrapBrush(value)
        );
    }


    private (ValueConverter ToShared, ValueConverter ToNative) GetPenMapper()
    {
        return (
            value => value is null ? null : _factory.WrapPen(value),
            value => value is null ? null : _factory.UnwrapPen(value)
        );
    }

    private record struct PropertyAccessor(Func<object?, Task> Setter, Func<object?> Getter,
        ValueConverter? ToShared = null, ValueConverter? ToNative = null);
}