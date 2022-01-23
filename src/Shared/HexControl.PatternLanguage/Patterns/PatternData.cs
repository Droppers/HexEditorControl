using System;
using System.Collections.Generic;
using System.Drawing;
using HexControl.Core;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Patterns;

public abstract class PatternData : IEquatable<PatternData>, ICloneable<PatternData>
{
    private static long _currentColor;
    private static readonly Color[] palette =
{
            System.Drawing.Color.Red,
                        System.Drawing.Color.Green,            System.Drawing.Color.Blue,
                                    System.Drawing.Color.Purple,
                                                System.Drawing.Color.Orange,
                                                            System.Drawing.Color.Yellow,
                                                                        System.Drawing.Color.Pink,
                                                                                    System.Drawing.Color.Maroon,
                                                                                                System.Drawing.Color.Cyan,
                                                                                                            System.Drawing.Color.LightGray,
        };

    protected PatternData(long offset, long size, Evaluator evaluator, uint color = 0)
    {
        _offset = offset;
        Size = size;
        Evaluator = evaluator;
        //Color = color; // TODO: Implement colors



        //if (color != 0)
        //{
        //    return;
        //}

        Color = palette[(int)(_currentColor++ % palette.Length)];

        //if (SharedData.patternPaletteOffset >= (palette.Length / sizeof(uint)))
        //{
        //    SharedData.patternPaletteOffset = 0;
        //}
    }

    protected PatternData(PatternData other)
    {
        Evaluator = other.Evaluator;
        _offset = other.Offset;
        Size = other.Size;
        Color = other.Color;
        Hidden = other.Hidden;
        Endian = other.Endian;
        VariableName = other.VariableName;
        TypeName = other.TypeName;
        Comment = other.Comment;
        Local = other.Local;
    }

    public abstract PatternData Clone();

    public string? VariableName { set; get; }

    public string? TypeName { set; get; }

    public Color? Color { set; get; }

    public Endianess Endian { set; get; } = Endianess.Native;

    public string? DisplayName
    {
        set => _displayName = value;
        get => _displayName ?? VariableName;
    }

    public virtual void CreateMarkers(List<Marker> markers)
    {
        markers.Add(new Marker(Offset, Size)
        {
            Background = Color ?? System.Drawing.Color.White,
            Foreground = Color is null ? System.Drawing.Color.Black : null,
            BehindText = true
        });
    }

    private Evaluator Evaluator { get; }

    public PatternFunctionBody? TransformFunction { set; get; }

    public PatternFunctionBody? FormatterFunction { set; get; }
        
    public abstract string GetFormattedName();

    public virtual string ToString(BaseBuffer buffer)
    {
        return $"{TypeName} {VariableName} @ 0x{Offset:X}";
    }
        
    public bool Local { get; set; }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public PatternData? Parent { get; set; }

    public bool Equals(PatternData? other)
    {
        if (other is null)
        {
            return false;
        }

        return Offset == other.Offset &&
               Size == other.Size &&
               Hidden == other.Hidden &&
               Endian == other.Endian &&
               VariableName == other.VariableName &&
               TypeName == other.TypeName &&
               Comment == other.Comment &&
               Local == other.Local;
    }


    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || Equals(obj as PatternData);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Offset, Size, Hidden, Endian, VariableName, TypeName, Comment,
            Local);
    }

    public string FormatDisplayValue(string value, Literal literal)
    {
        if (FormatterFunction is null)
        {
            return value;
        }
        else
        {
            var result = FormatterFunction(Evaluator, new[] {literal});

            if (result is not null)
            {
                if (result is StringLiteral str)
                {
                    return str.Value;
                }
                else
                {
                    return "???";
                }
            }
            else
            {
                return "???";
            }
        }
    }

    public bool Hidden { get; set; }

    private long _offset;
    public virtual long Offset { get => _offset; set => _offset = value; }
    public long Size { get; set; }

    private string? _displayName;
    public string? Comment { get; set; }
}