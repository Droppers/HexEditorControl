using System.Runtime.InteropServices;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host;
using HexControl.Framework.Host.Controls;
using HexControl.Framework.Visual;
using HexControl.Buffers;
using HexControl.Framework.Host.Events;
using HexControl.SharedControl.Documents;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorElement : VisualElement
{
    private const int SPACING_BETWEEN_COLUMNS = 2;

    private int _horizontalCharacterOffset;

    private readonly SharedHexControl _control;
    private readonly EditorRendererState _renderState;

    private EditorColumn _activeColumn = EditorColumn.Left;
    private DocumentConfiguration _configuration = DocumentConfiguration.Default;

    private Document? _document;

    private bool _keyboardSelectMode;
    private bool _mouseSelectMode;
    private SharedPoint? _mouseDownPosition;
    private long? _startSelectionOffset;
    private long? _keyDownOffset;

    private byte[] _bytes = Array.Empty<byte>();
    private long _bytesLength;

    public byte[] Bytes
    {
        set => _bytes = value;
    }

    public long BytesLength
    {
        set => _bytesLength = value;
    }

    public int HorizontalCharacterOffset
    {
        get => _horizontalCharacterOffset; set
        {
            _calculator.HorizontalCharacterOffset = value;
            _horizontalCharacterOffset = value;
        }
    }

    public IReadOnlyList<ModifiedRange> Modifications { get; set; }

    public ITextBuilder? TextBuilder { get; set; }

    private readonly EditorCalculator _calculator;

    public EditorElement(SharedHexControl control)
    {
        _control = control;
        _calculator = new EditorCalculator(_configuration, _horizontalCharacterOffset, false);
        _renderState = new EditorRendererState(this);

        Configuration = new DocumentConfiguration();
        Modifications = Array.Empty<ModifiedRange>();
    }

    public long MaxVisibleOffset
    {
        get
        {
            var height = Height - _control.HeaderHeight;
            var rows = height / _control.RowHeight;
            var offset = (long)rows * Configuration.BytesPerRow;
            offset = Offset + Math.Min(offset, _bytesLength);
            return (long)(Math.Ceiling(offset / (double)Configuration.BytesPerRow) * Configuration.BytesPerRow);
        }
    }

    public int VisibleRows
    {
        get
        {

            var height = Height - _control.HeaderHeight;
            return (int)(height / _control.RowHeight);
        }
    }

    public Document? Document
    {
        get => _document;
        set
        {
            _document = value;
            OnDocumentChanged();
        }
    }

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _calculator.Configuration = value;
            _configuration = value;
        }
    }

    public int TotalWidth =>
        _calculator.GetColumnWidth(EditorColumn.Left) +
        (Configuration.ColumnsVisible is VisibleColumns.DataText
            ? _calculator.GetColumnWidth(EditorColumn.Right) + SPACING_BETWEEN_COLUMNS
            : 0);

    public long Offset { get; set; }
    private bool CanModify => Document?.Buffer.IsReadOnly == false && Document?.Buffer.Locked == false;

    private int RowHeight => _control.RowHeight;
    private int CharacterWidth => _control.CharacterWidth;

    protected override void OnHostAttached(IHostControl attachHost)
    {
        InitTextBox();
    }

    private void InitTextBox()
    {
        var textBox = Host?.GetChild<IHostTextBox>(SharedHexControl.FakeTextBoxName);
        if (textBox is null)
        {
            return;
        }

        textBox.TextChanged += TextBoxOnTextChanged;
        textBox.KeyDown += TextBoxOnKeyDown;
        textBox.KeyUp += TextBoxOnKeyUp;
    }

    protected override void OnTreeAttached()
    {
        MouseDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseUp += OnMouseUp;
        MouseLeave += OnMouseLeave;
    }

    protected override void OnTreeDetached()
    {
        MouseDown -= OnMouseDown;
        MouseMove -= OnMouseMove;
        MouseUp -= OnMouseUp;
        MouseLeave -= OnMouseLeave;
    }

    private void OnDocumentChanged()
    {
        _calculator.Configuration = _configuration;
    }

    protected override unsafe void Render(IRenderContext context)
    {
        if (Document is null)
        {
            return;
        }

        Span<ModifiedRange> modifications = stackalloc ModifiedRange[Modifications.Count];
        if (Modifications is ModifiedRange[] array)
        {
            array.AsSpan().CopyTo(modifications);
        }
        else
        {
            CollectionsMarshal.AsSpan((List<ModifiedRange>)Modifications).CopyTo(modifications);
        }

        var state = Document.CapturedState;
        var calculator = new EditorCalculator(state.Configuration, _horizontalCharacterOffset, true);
        new EditorRenderer(
            _control,
            this,
            state,
            _renderState,
            context,
            TextBuilder,
            calculator,
            _bytes,
            _bytesLength).Render(modifications);
    }

    private (EditorColumn column, SharedPoint) GetPointRelativeToColumn(SharedPoint point)
    {
        var leftVisible = _calculator.GetVisibleColumnWidth(EditorColumn.Left) * CharacterWidth;
        var leftWidth = _calculator.GetColumnWidth(EditorColumn.Left) * CharacterWidth;
        var leftOffset = _calculator.GetLeft(_calculator.HorizontalColumnoffset, EditorColumn.Left) * CharacterWidth;

        var y = point.Y - _control.HeaderHeight;

        if (leftVisible < 0)
        {
            var x = _calculator.GetColumnWidth(EditorColumn.Right) * CharacterWidth -
                    (_calculator.GetVisibleColumnWidth(EditorColumn.Right) * CharacterWidth) + point.X;
            return (EditorColumn.Right, new SharedPoint(x, y));
        }
        else
        {
            var column = EditorColumn.Left;
            var x = leftOffset + Math.Max(0, point.X);
            if (x > leftWidth && Configuration.ColumnsVisible is VisibleColumns.DataText)
            {

                x = Math.Min(_calculator.GetColumnWidth(EditorColumn.Right) * CharacterWidth,
                    x - (leftWidth + SPACING_BETWEEN_COLUMNS * CharacterWidth));
                column = EditorColumn.Right;
            }

            return (column, new SharedPoint(x, y));
        }
    }

    private (EditorColumn side, long offset, int nibble) GetOffsetFromPoint(SharedPoint point)
    {
        var (column, relativePoint) = GetPointRelativeToColumn(point);
        var characterSet = _calculator.GetCharacterSetForColumn(column);
        var leftInCharacters = (int)(relativePoint.X / CharacterWidth);

        var groupCount = characterSet.Groupable
            ? leftInCharacters / (Configuration.GroupSize * characterSet.VisualWidth + 1)
            : 0;

        var byteColumn = (int)(((leftInCharacters - groupCount) / (double)characterSet.VisualWidth) * characterSet.ByteWidth);
        var nibble = Math.Max(0, ((int)relativePoint.X - (_calculator.GetLeft(byteColumn, column) * CharacterWidth)) / (double)_control.CharacterWidth);

        var byteRow = (int)(relativePoint.Y / RowHeight);
        var offset = Offset + (byteRow * Configuration.BytesPerRow + byteColumn);

        if (characterSet.VisualWidth is 1 && nibble >= 0.5 || Math.Round(nibble) >= characterSet.VisualWidth)
        {
            offset++;
            nibble = 0;
        }

        nibble = Math.Round(nibble);
        return (column, ClampOffset(offset), offset >= Document?.Length ? 0 : (int)nibble);
    }

    private void OnMouseDown(object? sender, HostMouseButtonEventArgs e)
    {
        if (e.Button is not HostMouseButton.Left || Document is null)
        {
            return;
        }

        var position = e.PointRelativeTo(this);
        if (!IsPointInEditableArea(position))
        {
            return; // Use clicked outside editable area, don't count this as valid click to prevent weird behavior
        }
        
        var (column, offset, _) = GetOffsetFromPoint(position);

        // Track the mouse down position for determining initial drag direction and whether it is a click or selection.
        _mouseDownPosition = position;
        _activeColumn = column;
        _startSelectionOffset = offset;

        CaptureMouse();
    }

    private long ClampOffset(long offset) => Math.Max(0, Math.Min(offset, Document?.Length ?? 0));

    private void OnMouseUp(object? sender, HostMouseButtonEventArgs e)
    {
        ReleaseMouse();

        if (Document is null)
        {
            return;
        }

        var (column, offset, nibble) = GetOffsetFromPoint(e.PointRelativeTo(this));
        if (_mouseDownPosition is not null)
        {
            var activeColumn = MapToActiveColumn(column);
            SetCaretOffset(activeColumn, offset, nibble);
        }

        _mouseDownPosition = null;
        _startSelectionOffset = null;
        _mouseSelectMode = false;
    }

    private void SetCaretOffset(long offset, int nibble = 0, bool scrollToCaret = false)
    {
        if (Document is null)
        {
            return;
        }

        SetCaretOffset(Document.Caret.Column, offset, nibble, scrollToCaret);
    }

    private void SetCaretOffset(ActiveColumn column, long offset, int nibble = 0, bool scrollToCaret = false)
    {
        if (Document is null)
        {
            return;
        }

        ResetCaretTick();
        Document.ChangeCaret(column, offset, nibble, scrollToCaret);
    }

    private bool IsPointInEditableArea(SharedPoint point)
    {
        var leftWidth = _calculator.GetVisibleColumnWidth(EditorColumn.Left) * CharacterWidth;
        var rightWidth = _calculator.GetVisibleColumnWidth(EditorColumn.Right) * CharacterWidth;

        var inLeftColumn = point.X < leftWidth;
        var pastHeader = point.Y > _control.HeaderHeight;

        // When the final row is completely filled, add a row to allow for inserting at the end
        var insertAtNextRow = _bytesLength % Configuration.BytesPerRow is 0; 
        var rowCount = Math.Ceiling(_bytesLength / (float)Configuration.BytesPerRow) + (insertAtNextRow ? 1 : 0);
        var beforeEnd = point.Y < rowCount * _control.RowHeight + _control.HeaderHeight;

        // Only left column is visible, don't check right column
        if (Configuration.ColumnsVisible is not VisibleColumns.DataText)
        {
            return inLeftColumn && pastHeader && beforeEnd;
        }

        if (leftWidth > 0)
        {
            var rightColumnX = leftWidth + SPACING_BETWEEN_COLUMNS * _control.CharacterWidth;
            var inRightColumn = point.X >= rightColumnX && point.X < rightColumnX + rightWidth;
            return (inLeftColumn || inRightColumn) && pastHeader && beforeEnd;
        }
        else
        {
            var inRightColumn = point.X >= 0 && point.X < rightWidth;
            return inRightColumn && pastHeader && beforeEnd;
        }
    }

    private void OnMouseMove(object? sender, HostMouseEventArgs e)
    {
        var position = e.PointRelativeTo(this);
        Cursor = IsPointInEditableArea(position) ? HostCursor.Text : null;

        if (_startSelectionOffset is null || Document is null || _keyboardSelectMode && !_mouseSelectMode)
        {
            return;
        }

        // Allow for some mouse movement tolerance before not considering it a mouse click
        if (_mouseDownPosition is not null && Math.Abs(position.X - _mouseDownPosition.Value.X) +
            Math.Abs(position.Y - _mouseDownPosition.Value.Y) < 2)
        {
            return;
        }

        var (column, offset, nibble) = GetOffsetFromPoint(position);

        // Allow selecting from middle of character rather than entire character
        var characterSet = _calculator.GetCharacterSetForColumn(column);
        if (nibble >= 1 && nibble >= characterSet.VisualWidth / 2)
        {
            offset += 1;
        }

        // Check if user is initially dragging backwards (left or up)
        if (_mouseDownPosition is not null)
        {
            var roundType = position.X < _mouseDownPosition.Value.X || position.Y < _mouseDownPosition.Value.Y
                ? EditorCalculator.RoundType.Ceil
                : EditorCalculator.RoundType.Floor;
            _startSelectionOffset = EditorCalculator.RoundTo(_startSelectionOffset.Value, _calculator.MaxByteWidth, roundType);
        }

        _mouseDownPosition = null;
        _mouseSelectMode = true;
        Select(offset, MapToActiveColumn(_activeColumn));
    }

    private void OnMouseLeave(object? sender, HandledEventArgs e)
    {
        Cursor = null;
    }
    
    private void Select(long newOffset, ActiveColumn column)
    {
        if (_startSelectionOffset is null || Document is null)
        {
            return;
        }

        var roundType = newOffset >= _startSelectionOffset
            ? EditorCalculator.RoundType.Ceil
            : EditorCalculator.RoundType.Floor;
        newOffset = EditorCalculator.RoundTo(newOffset, _calculator.MaxByteWidth, roundType);

        var startOffset = newOffset >= _startSelectionOffset.Value ? _startSelectionOffset.Value : newOffset;
        var endOffset = newOffset >= _startSelectionOffset.Value ? newOffset : _startSelectionOffset.Value;

        ResetCaretTick();

        if (startOffset == endOffset)
        {
            Document.Caret = Document.Caret with
            {
                Offset = newOffset,
                Nibble = 0
            };
            Document.Deselect();
        }
        else
        {
            var newCaretLocation = newOffset >= _startSelectionOffset.Value
                ? NewCaretLocation.SelectionEnd
                : NewCaretLocation.SelectionStart;

            var length = endOffset >= Document.Length ? Document.Length - startOffset : endOffset - startOffset;
            Document.Select(startOffset, length, column, newCaretLocation, true);
        }
    }

    private async void TextBoxOnKeyDown(object? sender, HostKeyEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        if (sender is IHostTextBox textBox)
        {
            textBox.Clear();
        }

        var caret = Document.Caret;
        var selection = Document.Selection;
        var ctrlPressed = (e.Modifiers & HostKeyModifier.Control) is not 0;
        var shitPressed = (e.Modifiers & HostKeyModifier.Shift) is not 0;
        if (e.Key is HostKey.Left or HostKey.Up or HostKey.Down or HostKey.Right)
        {
            if (!_keyboardSelectMode)
            {
                Deselect();
            }

            HandleArrowKey(Document, e.Key, !ctrlPressed);
        }
        else if (ctrlPressed && e.Key is HostKey.C)
        {
            _ = await Document.TryCopyAsync();
        }
        else if (ctrlPressed && e.Key is HostKey.V && CanModify)
        {
            _ = await Document.TryPasteAsync();
        }
        else if (ctrlPressed && e.Key is HostKey.A)
        {
            _startSelectionOffset = 0;
            Select(Document.Length, MapToActiveColumn(_activeColumn));
            _startSelectionOffset = null;
        }
        else if ((ctrlPressed && e.Key is HostKey.Y || (ctrlPressed && shitPressed && e.Key is HostKey.Z)) && CanModify)
        {
            if (Document.Buffer.CanRedo)
            {
                await Document.Buffer.RedoAsync();
            }
        }
        else if (ctrlPressed && e.Key is HostKey.Z && CanModify)
        {
            if (Document.Buffer.CanUndo)
            {
                await Document.Buffer.UndoAsync();
            }
        }
        else if (e.Key is HostKey.Shift)
        {
            // Respect user dragging up for continuation with keyboard controls
            var newStartOffset = (caret.Offset == selection?.Start ? selection.Value.End : selection?.Start) ??
                                 caret.Offset;
            _startSelectionOffset = newStartOffset;
            if (!selection.HasValue)
            {
                _keyDownOffset = newStartOffset;
            }
            _keyboardSelectMode = true;
        }
        else if (e.Key is HostKey.Back or HostKey.Delete && CanModify && selection.HasValue)
        {
            await Document.Buffer.DeleteAsync(selection.Value.Start, selection.Value.End - selection.Value.Start);
        }
        else if (caret.Nibble is 0 && CanModify)
        {
            switch (e.Key)
            {
                case HostKey.Back:
                    await Document.Buffer.DeleteAsync(caret.Offset - 1, 1);
                    break;
                case HostKey.Delete:
                    await Document.Buffer.DeleteAsync(caret.Offset, 1);
                    break;
            }
        }
    }

    private void TextBoxOnKeyUp(object? sender, HostKeyEventArgs e)
    {
        if (e.Key is not HostKey.Shift)
        {
            return;
        }

        if (!_mouseSelectMode)
        {
            _startSelectionOffset = null;
        }

        _keyboardSelectMode = false;
    }

    private async void TextBoxOnTextChanged(object? sender, HostTextChangedEventArgs e)
    {
        if (e.NewText.Length > 0)
        {
            await HandleWriteKey(e.NewText[^1]);
        }

        if (sender is IHostTextBox textBox)
        {
            textBox.Clear();
        }
    }

    private async ValueTask HandleWriteKey(char @char)
    {
        if (Document is null)
        {
            return;
        }

        _ = await Document.TryTypeAtCaretAsync(@char);
    }
    private void HandleArrowKey(Document document, HostKey key, bool jumpByte = false)
    {
        var offset = document.Caret.Offset;
        var nibble = document.Caret.Nibble;

        var characterSet = _calculator.GetCharacterSetForColumn(MapFromActiveColumn(document.Caret.Column));
        var maxDataWith = _calculator.GetCharacterSetForColumn(EditorColumn.Left).ByteWidth;

        if (_keyDownOffset.HasValue)
        {
            var roundType = key is HostKey.Right or HostKey.Down ? EditorCalculator.RoundType.Floor : EditorCalculator.RoundType.Ceil;
            _startSelectionOffset = EditorCalculator.RoundTo(_keyDownOffset.Value, _calculator.MaxByteWidth, roundType);
            offset = key is HostKey.Right or HostKey.Down ? _startSelectionOffset.Value + maxDataWith : _startSelectionOffset.Value - maxDataWith;
            _keyDownOffset = null;
        }
        else
        {
            var offsetIncrement = document.Selection.HasValue ? maxDataWith : characterSet.ByteWidth;

            // Allow for nibble level control when not selecting and byte level when selecting.
            switch (key)
            {
                case HostKey.Right when nibble == characterSet.VisualWidth - 1 || _keyboardSelectMode || jumpByte:
                    offset += offsetIncrement;
                    nibble = 0;
                    break;

                case HostKey.Left when nibble == 1 && jumpByte:
                    nibble--;
                    break;
                case HostKey.Left when _keyboardSelectMode || jumpByte:
                    offset -= offsetIncrement;
                    nibble = 0;
                    break;
                case HostKey.Left when nibble == 0:
                    offset -= offsetIncrement;
                    nibble = characterSet.VisualWidth - 1;
                    break;
                case HostKey.Left or HostKey.Right:
                    nibble += key is HostKey.Left ? -1 : 1;
                    break;
                case HostKey.Up or HostKey.Down:
                    offset += Configuration.BytesPerRow *
                              (key is HostKey.Up ? -1 : 1);
                    break;
            }
        }

        offset = Math.Max(0, Math.Min(document.Length, offset));
        nibble = Math.Max(0, nibble);

        if (_keyboardSelectMode is false && document.Selection is null)
        {
            SetCaretOffset(offset, nibble, true);
        }

        Select(offset, MapToActiveColumn(_activeColumn));
    }

    private void Deselect()
    {
        _startSelectionOffset = null;
        _keyboardSelectMode = false;

        Document?.Deselect();
    }

    private ActiveColumn MapToActiveColumn(EditorColumn column)
    {
        if (column is EditorColumn.Right && _configuration.ColumnsVisible is not VisibleColumns.DataText)
        {
            throw new InvalidOperationException(
                "Cannot map target column column when right column is not enabled.");
        }

        return column switch
        {
            EditorColumn.Left => _configuration.ColumnsVisible is VisibleColumns.Data or VisibleColumns.DataText
                ? ActiveColumn.Data
                : ActiveColumn.Text,
            EditorColumn.Right => _configuration.ColumnsVisible is VisibleColumns.DataText
                ? ActiveColumn.Text
                : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }

    private EditorColumn MapFromActiveColumn(ActiveColumn column)
    {
        return column switch
        {
            ActiveColumn.Data => _configuration.ColumnsVisible is VisibleColumns.Data or VisibleColumns.DataText
                ? EditorColumn.Left
                : EditorColumn.Right,
            ActiveColumn.Text => _configuration.ColumnsVisible is VisibleColumns.DataText
                ? EditorColumn.Right
                : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }

    private void ResetCaretTick()
    {
        _renderState.ResetCaretTick();
    }
}