using HexControl.Core.Characters;
using HexControl.Core.Observable;
using JetBrains.Annotations;

namespace HexControl.Core;

[PublicAPI]
public enum NumberBase
{
    Hexadecimal = 16,
    Decimal = 10,
    Octal = 8
}

public enum VisibleColumns
{
    HexText,
    Hex,
    Text
}

public class DocumentConfiguration : ObservableObject
{
    public static readonly DocumentConfiguration Default = new();

    private static readonly int[] ValidBytesPerRowValues = {8, 16, 24, 32, 48, 64, 128, 256, 512, 1024};
    private int _bytesPerRow = 16;
    private VisibleColumns _columnsVisible = VisibleColumns.HexText;
    private int _groupSize = 4;

    private CharacterSet _leftCharacterSet = new HexCharacterSet();
    private NumberBase _offsetBase = NumberBase.Decimal;
    private CharacterSet _rightCharacterSet = new TextCharacterSet(CharacterEncoding.Windows);


    private bool _showOffsets = true;

    public bool OffsetsVisible
    {
        get => Get(ref _showOffsets);
        set => Set(ref _showOffsets, value);
    }

    public VisibleColumns ColumnsVisible
    {
        get => Get(ref _columnsVisible);
        set => Set(ref _columnsVisible, value);
    }

    public NumberBase OffsetBase
    {
        get => Get(ref _offsetBase);
        set => Set(ref _offsetBase, value);
    }

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

    public int GroupSize
    {
        get => Get(ref _groupSize);
        set => Set(ref _groupSize, value);
    }


    public CharacterSet LeftCharacterSet
    {
        get => Get(ref _leftCharacterSet);
        set => Set(ref _leftCharacterSet, value);
    }

    public CharacterSet RightCharacterSet
    {
        get => Get(ref _rightCharacterSet);
        set => Set(ref _rightCharacterSet, value);
    }
}