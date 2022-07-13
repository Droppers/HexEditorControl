using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents;
using HexControl.Framework.Observable;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorCalculator : IDisposable
{
    private readonly SharedHexControl _control;
    private DocumentConfiguration _configuration = DocumentConfiguration.Default;
    private int _horizontalOffset;

    private int _horizontalCharacterOffset;
    private CharacterSet _leftCharacterSet = null!;
    private CharacterSet? _rightCharacterSet;

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _configuration.PropertyChanged -= OnPropertyChanged;
            _configuration = value;
            _configuration.PropertyChanged += OnPropertyChanged;

            OnConfigurationChanged();
        }
    }

    public int HorizontalOffset
    {
        get => _horizontalOffset;
        set
        {
            _horizontalOffset = Math.Max(0, Math.Min(value, GetColumnCharacterCount(ColumnSide.Left) + GetColumnCharacterCount(ColumnSide.Right) - 1));
            _horizontalCharacterOffset = GetFirstVisibleColumnIndex();
        }
    }

    public int HorizontalCharacterOffset
    {
        get => _horizontalCharacterOffset;
        set => _horizontalCharacterOffset = value;
    }

    public CharacterSet LeftCharacterSet
    {
        get => _leftCharacterSet;
        set => _leftCharacterSet = value;
    }

    public CharacterSet? RightCharacterSet
    {
        get => _rightCharacterSet;
        set => _rightCharacterSet = value;
    }

    public EditorCalculator(SharedHexControl control, DocumentConfiguration configuration, int horizontalOffset)
    {
        _control = control;
        Configuration = configuration;
        HorizontalOffset = horizontalOffset;
    }

    public int GetVisibleColumnWidth(ColumnSide column)
    {
        var startColumn = 0;
        if (column is ColumnSide.Left)
        {
            startColumn = _horizontalCharacterOffset;
        }
        else if (_horizontalCharacterOffset > _configuration.BytesPerRow)
        {
            startColumn = _horizontalCharacterOffset - _configuration.BytesPerRow;
        }

        return GetColumnCharacterCount(column) * _control.CharacterWidth - GetLeft(Math.Max(0, startColumn), column);
    }

    public int GetColumnCharacterCount(ColumnSide column)
    {
        var characterSet = GetCharacterSetForColumn(column);
        if (!characterSet.Groupable)
        {
            return _configuration.BytesPerRow * characterSet.Width;
        }

        return _configuration.BytesPerRow * characterSet.Width + _configuration.BytesPerRow / _configuration.GroupSize -
               1;
    }

    public int GetLeft(int offsetFromLeft, ColumnSide column, bool excludeLastGroup = false)
    {
        var isLastOfGroup = offsetFromLeft % _configuration.GroupSize is 0;
        var characterSet = GetCharacterSetForColumn(column);
        var groups = Math.Max(0, (characterSet.Groupable ? offsetFromLeft / _configuration.GroupSize : 0) -
                                 (excludeLastGroup && characterSet.Groupable && isLastOfGroup ? 1 : 0));
        return (offsetFromLeft * characterSet.Width + groups) * _control.CharacterWidth;
    }

    public int GetLeftRelativeToColumn(int offsetFromLeft, ColumnSide column, bool excludeLastGroup = false)
    {
        switch (column)
        {
            case ColumnSide.Left:
                return GetLeft(offsetFromLeft, column, excludeLastGroup) -
                       Math.Max(0, GetLeft(_horizontalCharacterOffset, column, excludeLastGroup));
            case ColumnSide.Right:
                var leftOffset = _horizontalCharacterOffset > _configuration.BytesPerRow
                    ? GetLeft(_horizontalCharacterOffset - _configuration.BytesPerRow, column, excludeLastGroup)
                    : 0;
                return GetLeft(offsetFromLeft, column, excludeLastGroup) - leftOffset;
            default:
                throw new ArgumentException("This column type is not supported.", nameof(column));
        }
    }

    public CharacterSet GetCharacterSetForColumn(ColumnSide column)
    {
        if (column is ColumnSide.Right && _configuration.ColumnsVisible is not VisibleColumns.HexText)
        {
            throw new InvalidOperationException(
                "Cannot get character set of right column when right column is not enabled.");
        }

        return column switch
        {
            ColumnSide.Left => _leftCharacterSet,
            ColumnSide.Right => _rightCharacterSet!,
            ColumnSide.Both => throw new ArgumentException(
                "Only a character set for either left or right columns can be determined.", nameof(column)),
            _ => throw new NotSupportedException("This column type is not supported.")
        };
    }

    private int GetFirstVisibleColumnIndex()
    {
        int GetVisibleCharacterCount(int horizontalOffset, CharacterSet characterSet)
        {
            var groups = characterSet.Groupable
                ? horizontalOffset / (_configuration.GroupSize * characterSet.Width + 1)
                : 0;
            return Math.Min((horizontalOffset - groups) / characterSet.Width, _configuration.BytesPerRow);
        }

        var leftCharacterCount = GetColumnCharacterCount(ColumnSide.Left);
        if (_rightCharacterSet is null)
        {
            return GetVisibleCharacterCount(HorizontalOffset, _leftCharacterSet);
        }

        return GetVisibleCharacterCount(HorizontalOffset, _leftCharacterSet) + Math.Max(0,
            GetVisibleCharacterCount(HorizontalOffset - leftCharacterCount, _rightCharacterSet));
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.Property is nameof(DocumentConfiguration.ColumnsVisible) or nameof(DocumentConfiguration.LeftCharacterSet)
            or nameof(DocumentConfiguration.RightCharacterSet))
        {
            UpdateCharacterSets();
        }
    }

    private void OnConfigurationChanged()
    {
        UpdateCharacterSets();
    }

    private void UpdateCharacterSets()
    {
        _leftCharacterSet = Configuration.ColumnsVisible is VisibleColumns.Hex or VisibleColumns.HexText
            ? Configuration.LeftCharacterSet
            : Configuration.RightCharacterSet;

        _rightCharacterSet = Configuration.ColumnsVisible is VisibleColumns.HexText
            ? Configuration.RightCharacterSet
            : null;
    }

    public void Dispose()
    {
        _configuration.PropertyChanged -= OnPropertyChanged;
    }
}