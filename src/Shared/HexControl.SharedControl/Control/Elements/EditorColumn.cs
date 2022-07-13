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

internal class EditorColumn : VisualElement
{
    private const int SPACING_BETWEEN_COLUMNS = 2;

    private readonly byte[] _readBuffer;
    private int _horizontalOffset;

    private readonly SharedHexControl _control;
    private readonly EditorRendererState _renderState;

    private ColumnSide _activeColumn = ColumnSide.Left;
    private DocumentConfiguration _configuration = DocumentConfiguration.Default;

    private Document? _document;


    private bool _keyboardSelectMode;
    private bool _mouseSelectMode;
    private SharedPoint? _mouseDownPosition;
    private long? _startSelectionOffset;

    private byte[] _bytes = Array.Empty<byte>();
    private long _bytesLength;

    public byte[] Bytes
    {
        get => _bytes; set
        {
            _bytes = value;
        }
    }
    public long BytesLength
    {
        get => _bytesLength; set
        {
            _bytesLength = value;
        }
    }

    public int HorizontalOffset
    {
        get => _horizontalOffset; set
        {
            _calculator.HorizontalOffset = value;
            _horizontalOffset = value;
        }
    }

    public IReadOnlyList<ModifiedRange> Modifications { get; set; }

    public ITextBuilder? TextBuilder { get; set; }

    private EditorCalculator _calculator;

    public EditorColumn(SharedHexControl control)
    {
        _control = control;
        _calculator = new EditorCalculator(_control, _configuration, _horizontalOffset);
        _renderState = new EditorRendererState(this);

        _readBuffer = new byte[8];

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
        _calculator.GetColumnCharacterCount(ColumnSide.Left) +
        (Configuration.ColumnsVisible is VisibleColumns.HexText
            ? _calculator.GetColumnCharacterCount(ColumnSide.Right) + SPACING_BETWEEN_COLUMNS
            : 0);

    public long Offset { get; set; }
    private bool CanModify => Document?.Buffer.IsReadOnly == true || Document?.Buffer.Locked == true;

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
        var oldCalculator = _calculator;
        _calculator = new EditorCalculator(_control, _configuration, _horizontalOffset);
        oldCalculator.Dispose();
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
        var calculator = new EditorCalculator(_control, state.Configuration, _horizontalOffset);
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

    private (ColumnSide column, SharedPoint) GetPointRelativeToColumn(SharedPoint point)
    {
        var leftWidth = _calculator.GetColumnCharacterCount(ColumnSide.Left) * CharacterWidth;
        var leftOffset = _calculator.GetLeft(_calculator.HorizontalCharacterOffset, ColumnSide.Left);

        var column = ColumnSide.Left;
        var x = leftOffset + Math.Max(0, point.X);
        if (x > leftWidth && Configuration.ColumnsVisible is VisibleColumns.HexText)
        {
            x = Math.Min(_calculator.GetColumnCharacterCount(ColumnSide.Right) * CharacterWidth,
                x - (leftWidth + SPACING_BETWEEN_COLUMNS * CharacterWidth));
            column = ColumnSide.Right;
        }

        return (column, new SharedPoint(x, point.Y - _control.HeaderHeight));
    }

    private (ColumnSide side, long offset, int nibble) GetOffsetFromPoint(SharedPoint point)
    {
        var (column, relativePoint) = GetPointRelativeToColumn(point);
        var characterSet = _calculator.GetCharacterSetForColumn(column);
        var leftInCharacters = (int)(relativePoint.X / CharacterWidth);

        var groupCount = characterSet.Groupable
            ? leftInCharacters / (Configuration.GroupSize * characterSet.Width + 1)
            : 0;

        var byteColumn = (leftInCharacters - groupCount) / characterSet.Width;
        var nibble = Math.Max(0, ((int)relativePoint.X - _calculator.GetLeft(byteColumn, column)) / _control.CharacterWidth);
        
        var byteRow = (int)(relativePoint.Y / RowHeight);
        var offset = Offset + (byteRow * Configuration.BytesPerRow + byteColumn);

        return (column, ClampOffset(offset), offset >= Document?.Length ? 0 : nibble);
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
            SetCaretOffset(column, offset, nibble);
            Deselect();
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

    private void SetCaretOffset(ColumnSide column, long offset, int nibble = 0, bool scrollToCaret = false)
    {
        if (Document is null)
        {
            return;
        }

        _renderState.ResetCaretTick();
        Document.ChangeCaret(column, offset, nibble, scrollToCaret);
    }

    private bool IsPointInEditableArea(SharedPoint point)
    {
        var leftWidth = _calculator.GetVisibleColumnWidth(ColumnSide.Left);
        var rightWidth = _calculator.GetVisibleColumnWidth(ColumnSide.Right);

        var inLeftColumn = point.X < leftWidth;
        var pastHeader = point.Y > _control.HeaderHeight;
        var rowCount = Math.Ceiling(_bytesLength / (float)Configuration.BytesPerRow);
        var beforeEnd = point.Y < rowCount * _control.RowHeight + _control.HeaderHeight;

        // Only left column is visible, don't check right column
        if (Configuration.ColumnsVisible is not VisibleColumns.HexText)
        {
            return inLeftColumn && pastHeader && beforeEnd;
        }

        var rightColumnX = leftWidth + SPACING_BETWEEN_COLUMNS * _control.CharacterWidth;
        var inRightColumn = leftWidth >= 0 && point.X >= rightColumnX && point.X < rightColumnX + rightWidth;
        return (inLeftColumn || inRightColumn) && pastHeader && beforeEnd;
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
        if (nibble >= characterSet.Width / 2)
        {
            offset += characterSet.Width / 2;
        }

        // Check if user is initially dragging backwards (left or up)
        if (_mouseDownPosition is not null &&
            (position.X < _mouseDownPosition.Value.X || position.Y < _mouseDownPosition.Value.Y))
        {
            _startSelectionOffset += 1;
        }

        _mouseDownPosition = null;
        _mouseSelectMode = true;
        Select(offset, _activeColumn);
    }

    private void OnMouseLeave(object? sender, HandledEventArgs e)
    {
        Cursor = null;
    }

    private void Select(long newOffset, ColumnSide column)
    {
        if (_startSelectionOffset is null || Document is null)
        {
            return;
        }

        var startOffset = newOffset >= _startSelectionOffset ? _startSelectionOffset.Value : newOffset;
        var endOffset = newOffset >= _startSelectionOffset ? newOffset : _startSelectionOffset.Value;

        if (startOffset == endOffset)
        {
            Document.Deselect();
        }
        else
        {
            var newCaretLocation = newOffset >= _startSelectionOffset.Value
                ? NewCaretLocation.SelectionEnd
                : NewCaretLocation.SelectionStart;
            Document.Select(startOffset, endOffset, column, newCaretLocation, true);
        }
    }

    private async void TextBoxOnKeyDown(object? sender, HostKeyEventArgs e)
    {
        if (Document is null)
        {
            return;
        }

        var caret = Document.Caret;
        var selection = Document.Selection;
        var ctrlPressed = (e.Modifiers & HostKeyModifier.Control) is not 0;
        if (e.Key is HostKey.Left or HostKey.Up or HostKey.Down or HostKey.Right)
        {
            if (!_keyboardSelectMode)
            {
                Deselect();
            }

            HandleArrowKeys(Document, e.Key, !ctrlPressed);
        }
        else if (ctrlPressed && e.Key is HostKey.A)
        {
            _startSelectionOffset = 0;
            Select(Document.Length, _activeColumn);
            _startSelectionOffset = null;
        }
        else if (ctrlPressed && e.Key is HostKey.Z && !CanModify && Document.Buffer.CanUndo)
        {
            await Document.Buffer.UndoAsync();
        }
        else if (ctrlPressed && e.Key is HostKey.Y && !CanModify && Document.Buffer.CanRedo)
        {
            await Document.Buffer.RedoAsync();
        }
        else if (e.Key is HostKey.Shift)
        {
            // Respect user dragging up for continuation with keyboard controls
            _startSelectionOffset =
                (caret.Offset == selection?.Start ? selection.Value.End - 1 : selection?.Start) ??
                caret.Offset;
            _keyboardSelectMode = true;
        }
        else if (selection.HasValue && e.Key is HostKey.Back or HostKey.Delete && !CanModify)
        {
            await Document.Buffer.DeleteAsync(selection.Value.Start, selection.Value.End - selection.Value.Start);
            SetCaretOffset(selection.Value.Start);
            Deselect();
        }
        else if (caret.Nibble is 0 && !CanModify)
        {
            switch (e.Key)
            {
                case HostKey.Back:
                    await Document.Buffer.DeleteAsync(caret.Offset - 1, 1);
                    SetCaretOffset(caret.Offset - 1);
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

    private async Task HandleWriteKey(char @char)
    {
        if (Document is null || CanModify)
        {
            return;
        }

        Deselect();

        var caret = Document.Caret;
        var appendToDocument = caret.Offset >= Document.Length;

        var characterSet = _calculator.GetCharacterSetForColumn(caret.Column);
        var oldByte = (byte)0;

        if (!appendToDocument)
        {
            var readByte = await ReadCaretByte(caret);
            if (readByte is null)
            {
                return;
            }

            oldByte = readByte.Value;
        }

        // Write to byte and validate if it is possible to write this character
        if (!characterSet.TryWrite(oldByte, @char, caret.Nibble, out var newByte))
        {
            return;
        }

        if (appendToDocument || Configuration.WriteInsert)
        {
            await Document.Buffer.InsertAsync(caret.Offset, newByte);
        }
        else
        {
            await Document.Buffer.WriteAsync(caret.Offset, newByte);
        }

        HandleArrowKeys(Document, HostKey.Right);
    }

    private async Task<byte?> ReadCaretByte(Caret caret)
    {
        if (Document is null)
        {
            return null;
        }

        var relativeOffset = caret.Offset - Offset;
        if (relativeOffset < _bytesLength)
        {
            return _bytes[relativeOffset];
        }

        // Allow for writing outside of current visible buffer
        var readLength = await Document.Buffer.ReadAsync(_readBuffer, caret.Offset);
        if (readLength <= 0)
        {
            return null;
        }

        return _readBuffer[0];
    }

    private void HandleArrowKeys(Document document, HostKey key, bool jumpByte = false)
    {
        var offset = document.Caret.Offset;
        var nibble = document.Caret.Nibble;

        // Allow for nibble level control when not selecting and byte level when selecting.
        var charset = _calculator.GetCharacterSetForColumn(document.Caret.Column);
        switch (key)
        {
            case HostKey.Right when nibble == charset.Width - 1 || _keyboardSelectMode || jumpByte:
                offset++;
                nibble = 0;
                break;

            case HostKey.Left when nibble == 1 && jumpByte:
                nibble--;
                break;
            case HostKey.Left when _keyboardSelectMode || jumpByte:
                offset--;
                nibble = 0;
                break;
            case HostKey.Left when nibble == 0:
                offset--;
                nibble = charset.Width - 1;
                break;
            case HostKey.Left or HostKey.Right:
                nibble += key is HostKey.Left ? -1 : 1;
                break;
            case HostKey.Up or HostKey.Down:
                offset += Configuration.BytesPerRow *
                          (key is HostKey.Up ? -1 : 1);
                break;
        }

        offset = Math.Max(0, Math.Min(document.Length, offset));
        nibble = Math.Max(0, nibble);

        if (document.Selection is null)
        {
            SetCaretOffset(offset, nibble, true);
        }

        Select(offset, _activeColumn);
    }

    private void Deselect()
    {
        _startSelectionOffset = null;
        _keyboardSelectMode = false;

        Document?.Deselect();
    }
}