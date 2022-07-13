using HexControl.Framework.Observable;
using HexControl.SharedControl.Characters;
using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public enum NumberBase
{
    Hexadecimal = 16,
    Decimal = 10,
    Octal = 8
}

[PublicAPI]
public enum VisibleColumns
{
    HexText,
    Hex,
    Text
}

[PublicAPI]
public class DocumentConfiguration : ObservableObject
{
    public static readonly DocumentConfiguration Default = new();

    private static readonly int[] ValidBytesPerRowValues = {8, 16, 24, 32, 48, 64, 128, 256, 512, 1024};

    private bool _offsetsVisible = true;
    public bool OffsetsVisible
    {
        get => Get(ref _offsetsVisible);
        set => Set(ref _offsetsVisible, value);
    }

    private VisibleColumns _columnsVisible = VisibleColumns.HexText;
    public VisibleColumns ColumnsVisible
    {
        get => Get(ref _columnsVisible);
        set => Set(ref _columnsVisible, value);
    }

    private NumberBase _offsetBase = NumberBase.Hexadecimal;
    public NumberBase OffsetBase
    {
        get => Get(ref _offsetBase);
        set => Set(ref _offsetBase, value);
    }

    private int _bytesPerRow = 16;
    public int BytesPerRow
    {
        get => Get(ref _bytesPerRow);
        set
        {
            if (!ValidBytesPerRowValues.Contains(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value),
                    $"Configuration 'BytesPerRow' must be a value of: {string.Join(',', ValidBytesPerRowValues)}.");
            }

            Set(ref _bytesPerRow, value);
        }
    }

    private int _groupSize = 4;
    public int GroupSize
    {
        get => Get(ref _groupSize);
        set => Set(ref _groupSize, value);
    }

    private CharacterSet _leftCharacterSet = new HexCharacterSet();

    public CharacterSet LeftCharacterSet
    {
        get => Get(ref _leftCharacterSet);
        set => Set(ref _leftCharacterSet, value);
    }

    private CharacterSet _rightCharacterSet = new TextCharacterSet(CharacterEncoding.Windows);
    public CharacterSet RightCharacterSet
    {
        get => Get(ref _rightCharacterSet);
        set => Set(ref _rightCharacterSet, value);
    }

    private bool _writeInsert;
    public bool WriteInsert
    {
        get => Get(ref _writeInsert);
        set => Set(ref _writeInsert, value);
    }
}