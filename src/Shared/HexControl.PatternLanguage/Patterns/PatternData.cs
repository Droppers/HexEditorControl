using System;
using System.Collections.Generic;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Literals;

namespace HexControl.PatternLanguage.Patterns;

public abstract class PatternData : IEquatable<PatternData>, ICloneable<PatternData>
{

    protected PatternData(long offset, long size, Evaluator evaluator, uint color = 0)
    {
        _offset = offset;
        Size = size;
        Evaluator = evaluator;
        Color = color;

        // TODO: implement automatic color selection
        //uint[] palette =
        //{
        //    0x70b4771f, 0x700e7fff, 0x702ca02c, 0x702827d6, 0x70bd6794, 0x704b568c, 0x70c277e3, 0x707f7f7f,
        //    0x7022bdbc, 0x70cfbe17
        //};

        //if (color != 0)
        //{
        //    return;
        //}

        //Color = palette[SharedData.patternPaletteOffset++];

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

    public uint Color { set; get; }

    public Endianess Endian { set; get; } = Endianess.Native;

    public string? DisplayName
    {
        set => _displayName = value;
        get => _displayName ?? VariableName;
    }

    private Evaluator Evaluator { get; }

    public PatternFunctionBody? TransformFunction { set; get; }

    public PatternFunctionBody? FormatterFunction { set; get; }
        
    public abstract string GetFormattedName();

    public virtual Dictionary<long, uint>? GetHighlightedAddresses()
    {
        if (Hidden)
        {
            return null;
        }

        Dictionary<long, uint> result = new();
        for (long i = 0; i < Size; i++)
            result.Add(Offset + i, Color);

        return result;
    }
        
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