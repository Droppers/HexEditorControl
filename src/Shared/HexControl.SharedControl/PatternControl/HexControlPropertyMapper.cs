using HexControl.PatternLanguage.Patterns;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.PatternControl;

namespace HexControl.SharedControl.Control;

internal class PatternControlPropertyMapper : PropertyMapper
{
    private readonly SharedPatternControl _control;
    private readonly NativeFactory _factory;

    public PatternControlPropertyMapper(SharedPatternControl control, NativeFactory factory)
    {
        _control = control;
        _factory = factory;
    }

    public override async Task SetValue(string propertyName, object? value)
    {
        switch (propertyName)
        {
            case nameof(_control.Patterns):
                _control.Patterns = CastNullable<List<PatternData>>(value);
                break;
        }
    }

    protected override TValue? GetValueInternal<TValue>(string? propertyName) where TValue : default
    {
        // TODO: ConvertBrushBack, ConvertPenBack, ConvertPrimitive
        return propertyName switch
        {
            nameof(_control.Patterns) => CastNullable<TValue>(_control.Patterns),
            _ => throw new ArgumentOutOfRangeException(nameof(propertyName), propertyName, null)
        };
    }
}