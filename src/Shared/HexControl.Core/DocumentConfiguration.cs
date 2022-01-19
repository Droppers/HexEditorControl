using System.Runtime.CompilerServices;
using HexControl.Core.Characters;

namespace HexControl.Core;

public enum Base
{
    Hexadecimal = 16,
    Decimal = 10,
    Octal = 8
}

public class ConfigurationChangedEventArgs : EventArgs
{
    public ConfigurationChangedEventArgs() { }


    public ConfigurationChangedEventArgs(string? property)
    {
        Property = property;
    }

    public string? Property { get; }
}

public abstract class ObservableObject
{
    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;

    protected static TField Get<TField>(ref TField field) => field;

    protected void Set<TField>(ref TField field, TField newValue, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<TField>.Default.Equals(field, newValue))
        {
            return;
        }

        field = newValue;

        OnConfigurationChanged(new ConfigurationChangedEventArgs(propertyName));
    }

    protected virtual void OnConfigurationChanged(ConfigurationChangedEventArgs e)
    {
        ConfigurationChanged?.Invoke(this, e);
    }
}

public enum VisibleColumns
{
    HexText,
    Hex,
    Text
}

public class DocumentConfiguration : ObservableObject
{
    private static readonly int[] ValidBytesPerRowValues = {8, 16, 24, 32, 48, 64, 128, 256, 512, 1024};
    private int _bytesPerRow = 32;
    private VisibleColumns _columnsVisible = VisibleColumns.HexText;
    private int _groupSize = 1;

    private CharacterSet _leftCharacterSet = new HexCharacterSet();
    private Base _offsetBase = Base.Decimal;
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

    public Base OffsetBase
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