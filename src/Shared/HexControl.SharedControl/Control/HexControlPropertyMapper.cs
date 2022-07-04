using HexControl.SharedControl.Framework.Host;
using HexControl.SharedControl.Framework.Mapping;

namespace HexControl.SharedControl.Control;

internal class HexControlPropertyMapper : PropertyMapper<SharedHexControl>
{
    public HexControlPropertyMapper(SharedHexControl control, INativeFactory factory) : base(control, factory)
    {
        AddAsyncNullable(nameof(control.Document), async value => await control.SetDocumentAsync(value), () => control.Document);
        Add(nameof(control.Foreground), value => control.Foreground = value, () => control.Foreground, MappingType.Brush);
        Add(nameof(control.EvenForeground), value => control.EvenForeground = value, () => control.EvenForeground, MappingType.Brush);
        Add(nameof(control.ScrollWheelSkipRows), value => control.ScrollWheelSkipRows = value, () => control.ScrollWheelSkipRows);
        Add(nameof(control.RowHeight), value => control.RowHeight = value, () => control.RowHeight);
    }
}