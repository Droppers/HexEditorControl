using System;
using System.Drawing;
using HexControl.Core;
using HexControl.PatternLanguage.Extensions;

namespace HexControl.PatternLanguage;

public class PatternMarker : IDocumentMarker
{
#pragma warning disable IDE1006 // Naming Styles
    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ConvertToConstant.Global
    public static Color[] GlobalPalette =
    {
        System.Drawing.Color.FromArgb(252, 92, 101),
        System.Drawing.Color.FromArgb(253, 150, 68),
        System.Drawing.Color.FromArgb(254, 211, 48),
        System.Drawing.Color.FromArgb(38, 222, 129),
        System.Drawing.Color.FromArgb(43, 203, 186),
        System.Drawing.Color.FromArgb(69, 170, 242),
        System.Drawing.Color.FromArgb(165, 94, 234),
        System.Drawing.Color.FromArgb(209, 216, 224),
        System.Drawing.Color.FromArgb(119, 140, 163),
        System.Drawing.Color.FromArgb(235, 59, 90),
        System.Drawing.Color.FromArgb(250, 130, 49),
        System.Drawing.Color.FromArgb(32, 191, 107),
        System.Drawing.Color.FromArgb(56, 103, 214),
        System.Drawing.Color.FromArgb(15, 185, 177),
        System.Drawing.Color.FromArgb(165, 177, 194),
        System.Drawing.Color.FromArgb(136, 84, 208),
        System.Drawing.Color.FromArgb(45, 152, 218),
        System.Drawing.Color.FromArgb(75, 101, 132),
        System.Drawing.Color.FromArgb(165, 177, 194),
    };

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ConvertToConstant.Global
    public static byte GlobalPaletteOpacity = 150;

    // ReSharper disable once FieldCanBeMadeReadOnly.Global
    // ReSharper disable once InconsistentNaming
    // ReSharper disable once ConvertToConstant.Global
    public static Color GlobalForegroundColor = Color.White;
#pragma warning restore IDE1006 // Naming Styles

    public PatternMarker(long offset, long length, int background)
    {
        Offset = offset;
        Length = length;
        var (r, g, b) = background.ToRgb();
        Background = Color.FromArgb(GlobalPaletteOpacity, r, g, b);
    }

    public Guid Id { get; set; }
    public long Offset { get; set; }
    public long Length { get; set; }
    public Color? Background { get; init; }

    public Color? Border
    {
        get => null;
        init { }
    }

    public Color? Foreground
    {
        get => null;//GlobalForegroundColor;
        init { }
    }

    public bool BehindText
    {
        get => true;
        init { }
    }

    public ColumnSide Column
    {
        get => ColumnSide.Both;
        init { }
    }
}