using System;
using System.Collections.Generic;
using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Extensions;
using HexControl.Core.Helpers;
using HexControl.PatternLanguage.Extensions;
using HexControl.PatternLanguage.Functions;
using HexControl.PatternLanguage.Helpers;
using HexControl.PatternLanguage.Literals;
using HexControl.PatternLanguage.Tokens;

namespace HexControl.PatternLanguage.Patterns;

/*
 * Data structure (8 + 4 + 2 + 8 + 8 + 1 + 1 = 32 bytes):
 * int64 StaticData: reference to common data
 * uint32 VariableNameIndex
 *     when _booleanValues IsArrayItem is set: index of array
 *     otherwise: index of string
 * uint16 TypeNameIndex
 *     if value is below 1000: Token.ValueType value, otherwise index of string
 * int64 _offset:
 *     7 bytes: actual offset
 *     1 byte: red color component
 * int64 _size:
 *     7 bytes: actual size
 *     1 byte: green color component
 * byte _colorB: blue color component
 * byte _booleanValues:
 *     1 bit: IsArrayItem
 *     1 bit: Local
 *     1 bit: unused
 *     1 bit: IsEndianSet
 *     1 bit: IsBigEndian
 *     1 bit: IsLittleEndian
 *     2 bits: unused
 */
public abstract class PatternData : IEquatable<PatternData>, ICloneable<PatternData>
{
    private static readonly StringDictionary TypeNameDictionary = new(1000, ConvertStringToValueType);
    private static readonly StringDictionary VariableNameDictionary = new(0);

    private static long _currentColor;

    private BooleanValue _booleanValues;
    private byte _colorB;
    private long _offset;
    private long _size;

    private StaticPatternData? _staticData;

    protected PatternData(long offset, long size, Evaluator evaluator, int color = 0)
    {
        _offset = offset.StoreLargeValue(offset);
        _size = size.StoreLargeValue(size);

        // Do not set this.Color since it is a virtual property
        if (color is not 0)
        {
            SetValue(BooleanValue.UserDefinedColor, true);
            SetColor(color);
        }
        else
        {
            SetValue(BooleanValue.UserDefinedColor, false);
            var automaticColor =
                PatternMarker.GlobalPalette[(int)(_currentColor++ % PatternMarker.GlobalPalette.Length)];
            SetColor(ColorExtensions.FromRgb(automaticColor.R, automaticColor.G, automaticColor.B));
        }
    }

    protected PatternData(PatternData other)
    {
        _offset = other.Offset;
        Size = other.Size;

        SetColor(other.Color);

        _booleanValues = other._booleanValues;
        VariableNameIndex = other.VariableNameIndex;
        TypeNameIndex = other.TypeNameIndex;

        Local = other.Local;
        StaticData = other.StaticData;
    }

    internal StaticPatternData StaticData
    {
        get => _staticData ?? throw new InvalidOperationException("StaticData for pattern has not been set.");
        set => _staticData = value;
    }

    public bool IsArrayItem => GetValue(BooleanValue.IsArrayItem);

    public int ArrayIndex
    {
        get => IsArrayItem ? (int)VariableNameIndex : -1;
        set
        {
            SetValue(BooleanValue.IsArrayItem, true);
            VariableNameIndex = (uint)value;
        }
    }

    public uint VariableNameIndex { get; set; }

    public string? VariableName
    {
        get =>
            IsArrayItem
                ? $"[{VariableNameIndex}]"
                : VariableNameDictionary.Get(VariableNameIndex);
        set
        {
            if (GetValue(BooleanValue.IsArrayItem))
            {
                return;
            }

            VariableNameIndex = VariableNameDictionary.AddOrGet(value);
        }
    }

    public ushort TypeNameIndex { get; set; }

    public string? TypeName
    {
        get
        {
            if (TypeNameIndex == TypeNameDictionary.NullIndex)
            {
                return null;
            }

            if (TypeNameIndex >= TypeNameDictionary.FirstUsableIndex)
            {
                return TypeNameDictionary.Get(TypeNameIndex);
            }

            return Token.GetTypeName((Token.ValueType)TypeNameIndex);
        }
        set => TypeNameIndex = (ushort)TypeNameDictionary.AddOrGet(value);
    }

    public string? DisplayName => StaticData.DisplayName ?? VariableName;

    public FunctionBody? TransformFunction => StaticData.TransformFunction;

    public FunctionBody? FormatterFunction => StaticData.FormatterFunction;

    public string? Comment => StaticData.Comment;

    public bool Local
    {
        get => GetValue(BooleanValue.Local);
        set => SetValue(BooleanValue.Local, value);
    }

    public bool Hidden
    {
        get => StaticData.Hidden;
        set => StaticData.Hidden = value;
    }

    public bool? Inlined
    {
        get => this is IPatternInlinable ? StaticData.Inlined : null;
        set
        {
            if (value is not null)
            {
                StaticData.Inlined = value.Value;
            }
        }
    }

    public Endianess? Endian
    {
        get
        {
            if (!GetValue(BooleanValue.IsEndianSet))
            {
                return null;
            }

            if (GetValue(BooleanValue.IsBigEndian))
            {
                return Endianess.Big;
            }

            return GetValue(BooleanValue.IsLittleEndian) ? Endianess.Little : Endianess.Native;
        }
        set
        {
            if (Endian is null)
            {
                SetValue(BooleanValue.IsEndianSet, false);
                SetValue(BooleanValue.IsBigEndian, false);
                SetValue(BooleanValue.IsLittleEndian, false);
                return;
            }

            SetValue(BooleanValue.IsEndianSet, true);
            if (value is Endianess.Big or Endianess.Little)
            {
                SetValue(BooleanValue.IsBigEndian, value is Endianess.Big);
                SetValue(BooleanValue.IsLittleEndian, value is Endianess.Little);
            }
            else
            {
                SetValue(BooleanValue.IsBigEndian, false);
                SetValue(BooleanValue.IsLittleEndian, false);
            }
        }
    }

    // ReSharper disable once UnusedAutoPropertyAccessor.Global
    public PatternData? Parent
    {
        get => null;
        set { }
    }

    public virtual long Offset
    {
        get => _offset.GetLargeValue();
        set => _offset = _offset.StoreLargeValue(value);
    }

    public long Size
    {
        get => _size.GetLargeValue();
        set => _size = _size.StoreLargeValue(value);
    }

    public int ColorR => _offset.GetSmallValue();
    public int ColorG => _size.GetSmallValue();
    public int ColorB => _colorB;

    public virtual int Color
    {
        get => ColorExtensions.FromRgb(_offset.GetSmallValue(), _size.GetSmallValue(), _colorB);
        set => SetColor(value);
    }

    public bool UserDefinedColor => GetValue(BooleanValue.UserDefinedColor);

    public abstract PatternData Clone();

    public bool Equals(PatternData? other)
    {
        if (other is null)
        {
            return false;
        }

        return Offset == other.Offset &&
               Size == other.Size &&
               Endian == other.Endian &&
               VariableName == other.VariableName &&
               TypeName == other.TypeName &&
               Hidden == other.Hidden &&
               Local == other.Local;
    }

    private void SetColor(int color)
    {
        var (r, g, b) = color.ToRgb();
        _offset = _offset.StoreSmallValue(r); // high byte store red component
        _size = _size.StoreSmallValue(g); // high byte stores blue component
        _colorB = b;
    }

    private static uint ConvertStringToValueType(StringDictionary dictionary, string str)
    {
        var value = Token.GetTypeFromName(str);
        return value is not null ? (ushort)value : dictionary.NullIndex;
    }

    protected bool GetValue(BooleanValue type) => (_booleanValues & type) == type;

    protected void SetValue(BooleanValue type, bool value)
    {
        if (value)
        {
            _booleanValues |= type;
        }
        else
        {
            _booleanValues &= ~type;
        }
    }

    public virtual void CreateMarkers(List<PatternMarker> markers)
    {
        var sameAsLast = false;
        if (markers.Count > 0)
        {
            var last = markers[^1];
            sameAsLast = last.Offset + last.Length == Offset && last.Background?.R == ColorR &&
                         last.Background?.G == ColorG &&
                         last.Background?.B == ColorB;
            if (sameAsLast)
            {
                last.Length += Size;
            }
        }

        if (!sameAsLast)
        {
            markers.Add(new PatternMarker(Offset, Size, Color));
        }
    }

    public abstract string GetFormattedName();

    public virtual string ToString(Evaluator evaluator) => $"{TypeName} {VariableName} @ 0x{Offset:X}";


    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || Equals(obj as PatternData);

    public override int GetHashCode() =>
        HashCode.Combine(Offset, Size, Hidden, Endian, VariableName, TypeName, Comment,
            Local);

    public string FormatDisplayValue(Evaluator evaluator, string value, Literal literal)
    {
        if (FormatterFunction is null)
        {
            return value;
        }

        var result = FormatterFunction(evaluator, new[] {literal});

        if (result is not null)
        {
            if (result is StringLiteral str)
            {
                return str.Value;
            }

            return "???";
        }

        return "???";
    }

    internal class StaticPatternData
    {
        public string? DisplayName { get; set; }

        public string? Comment { get; set; }

        public FunctionBody? TransformFunction { get; set; }

        public FunctionBody? FormatterFunction { get; set; }

        public bool Hidden { get; set; }

        public bool Inlined { get; set; }
    }

    [Flags]
    protected enum BooleanValue : byte
    {
        IsArrayItem = 1,
        Local = 2,
        UserDefinedColor = 4,
        IsEndianSet = 8,
        IsBigEndian = 16,
        IsLittleEndian = 32
    }
}