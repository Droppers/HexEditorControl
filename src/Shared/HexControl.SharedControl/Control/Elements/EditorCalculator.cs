using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents;
using System.Data.Common;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorCalculator
{
    private DocumentConfiguration _configuration = DocumentConfiguration.Default;
    private int _horizontalCharacterOffset;

    private int _horizontalColumnOffset;
    private CharacterSet _leftCharacterSet = null!;
    private CharacterSet? _rightCharacterSet;

    public DocumentConfiguration Configuration
    {
        set
        {
            _configuration = value;
            OnConfigurationChanged();
        }
    }

    public int HorizontalCharacterOffset
    {
        get => _horizontalCharacterOffset;
        set
        {
            var maxHorizontalOffset = GetColumnWidth(EditorColumn.Left) +
                (_configuration.ColumnsVisible is VisibleColumns.DataText ? GetColumnWidth(EditorColumn.Right) : 0) - 1;
            _horizontalCharacterOffset = Math.Max(0, Math.Min(value, maxHorizontalOffset));
            _horizontalColumnOffset = GetColumnInvisibleWidth();
        }
    }

    public int HorizontalColumnoffset => _horizontalColumnOffset;

    public CharacterSet LeftCharacterSet => _leftCharacterSet;

    public CharacterSet? RightCharacterSet => _rightCharacterSet;

    public int MaxDataWidth { get; private set; } = 1;

    public EditorCalculator(DocumentConfiguration configuration, int horizontalCharacterOffset,
        bool shortLifeSpan)
    {
        if (shortLifeSpan)
        {
            _configuration = configuration;
            OnConfigurationChanged();
        }
        else
        {
            Configuration = configuration;
        }

        HorizontalCharacterOffset = horizontalCharacterOffset;
    }

    public int GetVisibleColumnWidth(EditorColumn column)
    {
        if (column is EditorColumn.Right && _configuration.ColumnsVisible is not VisibleColumns.DataText)
        {
            return 0;
        }

        var startColumn = 0;
        if (column is EditorColumn.Left)
        {
            startColumn = _horizontalColumnOffset;
        }
        else if (_horizontalColumnOffset > _configuration.BytesPerRow)
        {
            startColumn = _horizontalColumnOffset - _configuration.BytesPerRow;
        }

        return GetColumnWidth(column) - GetLeft(Math.Max(0, startColumn), column);
    }

    public int GetColumnWidth(EditorColumn column)
    {
        var characterSet = GetCharacterSetForColumn(column);
        var bytesPerRow = _configuration.BytesPerRow / characterSet.DataWidth;
        if (!characterSet.Groupable)
        {
            return bytesPerRow * characterSet.Width;
        }

        return bytesPerRow * characterSet.Width + bytesPerRow / _configuration.GroupSize - 1;
    }

    public int GetLeft(int offsetFromLeft, EditorColumn column, bool excludeLastGroup = false)
    {
        var isLastOfGroup = offsetFromLeft % _configuration.GroupSize is 0;
        var characterSet = GetCharacterSetForColumn(column);
        offsetFromLeft /= characterSet.DataWidth;
        var groups = Math.Max(0, (characterSet.Groupable ? offsetFromLeft / _configuration.GroupSize : 0) -
                                 (excludeLastGroup && characterSet.Groupable && isLastOfGroup ? 1 : 0));
        return (offsetFromLeft * characterSet.Width + groups);
    }

    public int GetLeftRelativeToColumn(int offsetFromLeft, EditorColumn column, bool excludeLastGroup = false)
    {
        switch (column)
        {
            case EditorColumn.Left:
                return GetLeft(offsetFromLeft, column, excludeLastGroup) -
                       Math.Max(0, GetLeft(_horizontalColumnOffset, column));
            case EditorColumn.Right:
                var leftOffset = _horizontalColumnOffset > _configuration.BytesPerRow
                    ? GetLeft(_horizontalColumnOffset - _configuration.BytesPerRow, column)
                    : 0;
                return GetLeft(offsetFromLeft, column, excludeLastGroup) - leftOffset;
            default:
                throw new ArgumentException("This column type is not supported.", nameof(column));
        }
    }

    public CharacterSet GetCharacterSetForColumn(EditorColumn column)
    {
        if (column is EditorColumn.Right && _configuration.ColumnsVisible is not VisibleColumns.DataText)
        {
            throw new InvalidOperationException(
                "Cannot get character set of right column when right column is not enabled.");
        }

        return column switch
        {
            EditorColumn.Left => _leftCharacterSet,
            EditorColumn.Right => _rightCharacterSet!,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }

    private int GetColumnInvisibleWidth()
    {
        int GetVisibleColumnCount(int HorizontalCharacterOffset, CharacterSet characterSet)
        {
            var groups = characterSet.Groupable
                ? HorizontalCharacterOffset / (_configuration.GroupSize * characterSet.Width + 1)
                : 0;
            return Math.Min((HorizontalCharacterOffset - groups) / characterSet.Width, _configuration.BytesPerRow / characterSet.DataWidth);
        }

        var leftColumnWidth = GetColumnWidth(EditorColumn.Left);
        if (_rightCharacterSet is null)
        {
            return GetVisibleColumnCount(HorizontalCharacterOffset, _leftCharacterSet) * _leftCharacterSet.DataWidth;
        }

        return (GetVisibleColumnCount(HorizontalCharacterOffset, _leftCharacterSet) * _leftCharacterSet.DataWidth) + Math.Max(0,
            GetVisibleColumnCount(HorizontalCharacterOffset - leftColumnWidth, _rightCharacterSet) * _rightCharacterSet.DataWidth);
    }

    public long RoundToMaxDataWidth(long offset, RoundType type)
    {
        return type switch
        {
            RoundType.Ceil => (long)(Math.Ceiling(offset / (double)MaxDataWidth) * MaxDataWidth),
            RoundType.Floor => (long)(Math.Floor(offset / (double)MaxDataWidth) * MaxDataWidth),
            RoundType.Middle => (long)(Math.Round(offset / (double)MaxDataWidth) * MaxDataWidth),
            _ => throw new NotImplementedException()
        };
    }

    private void OnConfigurationChanged()
    {
        UpdateCharacterSets();

        MaxDataWidth = Math.Max(_leftCharacterSet.DataWidth, _rightCharacterSet?.DataWidth ?? 0);
    }

    private void UpdateCharacterSets()
    {
        _leftCharacterSet = _configuration.ColumnsVisible is VisibleColumns.Data or VisibleColumns.DataText
            ? _configuration.DataCharacterSet
            : _configuration.TextCharacterSet;

        _rightCharacterSet = _configuration.ColumnsVisible is VisibleColumns.DataText
            ? _configuration.TextCharacterSet
            : null;
    }

    public enum RoundType
    {
        Ceil,
        Floor,
        Middle
    }
}