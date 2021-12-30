using HexControl.Core;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Host;

namespace HexControl.SharedControl.Control;

internal class HexControlPropertyMapper : PropertyMapper
{
    private readonly SharedHexControl _control;
    private readonly NativeFactory _factory;

    public HexControlPropertyMapper(SharedHexControl control, NativeFactory factory)
    {
        _control = control;
        _factory = factory;
    }

    public override async Task SetValue(string propertyName, object? value)
    {
        if (propertyName == nameof(_control.Document))
        {
            await _control.SetDocumentAsync((Document?)value);
            return;
        }

        switch (propertyName)
        {
            case nameof(_control.RenderApi):
                _control.RenderApi = CastNullable<HexRenderApi>(value);
                break;
            case nameof(_control.EvenForeground) when value is not null:
                _control.EvenForeground = _factory.WrapBrush(value);
                break;
            case nameof(_control.Foreground) when value is not null:
                _control.Foreground = _factory.WrapBrush(value);
                break;
        }
    }

    protected override TValue? GetValueInternal<TValue>(string? propertyName) where TValue : default
    {
        // TODO: ConvertBrushBack, ConvertPenBack, ConvertPrimitive
        return propertyName switch
        {
            nameof(_control.RenderApi) => CastNullable<TValue>(_control.RenderApi),
            nameof(_control.RowHeight) => Cast<TValue>(_control.RowHeight),
            nameof(_control.Foreground) => _factory.ConvertObjectToNative<TValue>(_control.Foreground),
            nameof(_control.EvenForeground) => _factory.ConvertObjectToNative<TValue>(_control.EvenForeground),
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null)
        };
    }
}