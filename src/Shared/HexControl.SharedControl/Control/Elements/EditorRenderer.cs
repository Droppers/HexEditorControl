using HexControl.Buffers;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Control.Helpers;
using HexControl.SharedControl.Documents;
using System.Drawing;

namespace HexControl.SharedControl.Control.Elements;

internal readonly ref struct EditorRenderer
{
    private const int SPACING_BETWEEN_COLUMNS = 2;
    
    private readonly SharedHexControl _control;
    private readonly VisualElement _owner;
    private readonly CapturedState _documentState;
    private readonly EditorRendererState _renderState;
    private readonly IRenderContext _context;
    private readonly ITextBuilder? _textBuilder;
    private readonly EditorCalculator _calculator;
    private readonly byte[] _bytes;
    private readonly long _bytesLength;

    public EditorRenderer(
        SharedHexControl control,
        VisualElement owner,
        CapturedState documentState,
        EditorRendererState renderState,
        IRenderContext context,
        ITextBuilder? textBuilder,
        EditorCalculator calculator,
        byte[] bytes,
        long bytesLength)
    {
        _control = control;
        _owner = owner;
        _documentState = documentState;
        _renderState = renderState;
        _context = context;
        _textBuilder = textBuilder;
        _calculator = calculator;
        _bytes = bytes;
        _bytesLength = bytesLength;
    }

    public void Render(ReadOnlySpan<ModifiedRange> modifications)
    {
        // Resize lookup table containing custom foreground colors for each offset
        ResizeForegroundLookup();

        // Different foreground for modified bytes
        UpdateModificationBrushOverrides(modifications);

        // Draw the markers that should be drawn behind the text
        UpdateSelectionMarkers();
        DrawMarkers(marker => marker.BehindText);


        if (_textBuilder is not null)
        {
            _textBuilder.Clear();
            if (_documentState.Configuration.ColumnsVisible is VisibleColumns.Data or VisibleColumns.DataText &&
                _calculator.LeftCharacterSet.Groupable)
            {
                WriteHexOffsetHeader(_textBuilder);
            }

            if (_documentState.Configuration.ColumnsVisible is VisibleColumns.Text or VisibleColumns.DataText)
            {
                WriteTextHeader(_textBuilder);
            }

            if (_bytes.Length is not 0 && _bytesLength is not 0)
            {
                WriteContentBytes(_textBuilder);
            }

            _textBuilder.Draw(_context);
        }

        // Draw the markers that should be drawn in front of the text
        DrawMarkers(marker => !marker.BehindText);

        // Draw the carets
        DrawCarets();
    }

    #region Preparation
    private void ResizeForegroundLookup()
    {
        if (_bytesLength * 2 > _renderState.MarkerForegroundLookup.Length)
        {
            _renderState.MarkerForegroundLookup = new ISharedBrush[_bytesLength * 2];
        }
        else
        {
            Array.Clear(_renderState.MarkerForegroundLookup, 0, _renderState.MarkerForegroundLookup.Length);
        }
    }

    private void UpdateModificationBrushOverrides(ReadOnlySpan<ModifiedRange> modifications)
    {
        var count = modifications.Length;
        var rightOffset = _renderState.MarkerForegroundLookup.Length / 2;
        for (var i = 0; i < count; i++)
        {
            var modification = modifications[i];
            var startOffset = Math.Max(0, modification.StartOffset - _documentState.Offset);
            var length = Math.Min(_bytesLength - startOffset,
                modification.Length - (modification.StartOffset < _documentState.Offset ? _documentState.Offset - modification.StartOffset : 0));
            for (var j = 0; j < length; j++)
            {
                _renderState.MarkerForegroundLookup[startOffset + j] = _control.ModifiedForeground;
                _renderState.MarkerForegroundLookup[rightOffset + startOffset + j] = _control.ModifiedForeground;
            }
        }
    }

    private void UpdateSelectionMarkers()
    {
        if (_documentState.Selection is not { } selection)
        {
            return;
        }

        var editorColumn = MapFromActiveColumn(selection.Column);

        // Active
        _renderState.ActiveMarker ??= new Marker(selection.Start, selection.Length);
        _renderState.ActiveMarker.Offset = selection.Start;
        _renderState.ActiveMarker.Length = selection.Length;
        _renderState.ActiveMarker.Foreground = Color.White;
        _renderState.ActiveMarker.Background = Color.FromArgb(255, 21, 103, 210);
        _renderState.ActiveMarker.BehindText = true;
        _renderState.ActiveMarker.Column = editorColumn is EditorColumn.Left ? MarkerColumn.Data : MarkerColumn.Text;

        // Inactive
        _renderState.InactiveMarker ??= new Marker(selection.Start, selection.Length);
        _renderState.InactiveMarker.Offset = selection.Start;
        _renderState.InactiveMarker.Length = selection.Length;
        _renderState.InactiveMarker.Border = Color.FromArgb(255, 21, 103, 210);
        _renderState.InactiveMarker.Background = Color.FromArgb(100, 21, 103, 210);
        _renderState.InactiveMarker.BehindText = true;
        _renderState.InactiveMarker.Column = editorColumn is EditorColumn.Right ? MarkerColumn.Data : MarkerColumn.Text;
    }
    #endregion

    #region Markers
    private void DrawMarkers(Func<Marker, bool> condition)
    {
        if (_context.DirtyRect && ShouldAddMarkerDirtyRect())
        {
            _owner.AddDirtyRect(new SharedRectangle(0, 0, _owner.Width, _owner.Height), _control.CharacterWidth);
        }

        _renderState.PreviousMarkers.Clear();

        var span = _documentState.Markers.Span;
        var count = span.Length;
        for (var i = 0; i < count; i++)
        {
            var marker = span[i];
            if (!marker.IsVisible(_documentState.Offset, _bytesLength))
            {
                continue;
            }

            DrawMarker(marker, condition, false);
        }

        if (_documentState.Selection is { } selection && selection.End >= _documentState.Offset &&
            selection.Start <= _documentState.Offset + _bytesLength && selection.Length > 0)
        {
            DrawMarker(_renderState.ActiveMarker!, condition, true);
            DrawMarker(_renderState.InactiveMarker!, condition, true);
        }
    }

    private void DrawMarker(Marker marker, Func<Marker, bool> condition, bool checkVisibility)
    {
        if (!checkVisibility && !marker.IsVisible(_documentState.Offset, _bytesLength))
        {
            return;
        }

        _renderState.PreviousMarkers[marker] = new MarkerRange(marker.Offset, marker.Length);

        if (!condition(marker))
        {
            return;
        }

        UpdateMarkerBrushOverrides(marker);

        DrawLeftMarker(marker);
        DrawRightMarker(marker);

        _renderState.PreviousMarkers[marker] = new MarkerRange(marker.Offset, marker.Length);
    }

    private void DrawLeftMarker(Marker marker)
    {
        if (_calculator.HorizontalCharacterOffset >= _documentState.Configuration.BytesPerRow ||
            marker.Column is not (MarkerColumn.Data or MarkerColumn.DataText))
        {
            return;
        }

        DrawMarkerArea(EditorColumn.Left, marker);
    }

    private void DrawRightMarker(Marker marker)
    {
        if (marker.Column is MarkerColumn.Data)
        {
            return;
        }

        if (_calculator.HorizontalCharacterOffset < _documentState.Configuration.BytesPerRow)
        {
            var leftOffset = (_calculator.GetVisibleColumnWidth(EditorColumn.Left) + SPACING_BETWEEN_COLUMNS) * _control.CharacterWidth;
            _context.PushTranslate(leftOffset, 0);
        }

        if (_documentState.Configuration.ColumnsVisible is VisibleColumns.DataText)
        {
            DrawMarkerArea(EditorColumn.Right, marker);
        }

        if (_calculator.HorizontalCharacterOffset < _documentState.Configuration.BytesPerRow)
        {
            _context.Pop();
        }
    }

    private void DrawMarkerArea(EditorColumn column, Marker marker)
    {
        if (marker.Background is null && marker.Border is null)
        {
            return;
        }

        var position = CalculateMarkerPosition(marker.Offset, marker.Length, column);

        var brush = marker.Background is { } background ? _renderState.ColorToBrushCache[background] : null;
        var pen = marker.Border is { } border ? new SharedPen(_renderState.ColorToBrushCache[border]!, 1) : null;

        var square = position.StartOffset % _documentState.Configuration.BytesPerRow is 0 && 
                     (position.EndOffset + 1) % _documentState.Configuration.BytesPerRow is 0;
        if (position.StartRow == position.EndRow || marker.Length <= _documentState.Configuration.BytesPerRow || square)
        {
            DrawMarkerAreaSimple(brush, pen, column, marker, position);
        }
        else
        {
            DrawMarkerAreaAdvanced(brush, pen, column, position);
        }
    }

    private MarkerPosition CalculateMarkerPosition(long markerOffset, long markerLength, EditorColumn column)
    {
        if (column is EditorColumn.Left)
        {
        }
        var state = _documentState;
        var bytesPerRow = _documentState.Configuration.BytesPerRow; //;_calculator.GetBytesPerRow(column);
        
        var startOffset = Math.Max(0, markerOffset - _documentState.Offset);
        var length = Math.Min(_bytesLength, markerLength - (markerOffset < _documentState.Offset ? _documentState.Offset - markerOffset : 0) - 1);
        var endOffset = startOffset + length;
        var startRow = startOffset / bytesPerRow;
        var endRow = endOffset / bytesPerRow;

        var startX = Math.Max(0, _calculator.GetLeftRelativeToColumn((int)(startOffset % bytesPerRow), column) * _control.CharacterWidth);
        var startY = startOffset / bytesPerRow * _control.RowHeight + _control.HeaderHeight;

        var characterSet = _calculator.GetCharacterSetForColumn(column);

        var startColumn = startOffset % bytesPerRow;
        var extendPastEdge = characterSet.Groupable && (startColumn % _documentState.Configuration.GroupSize is 0 || !IsOffsetVisibleHorizontally(startColumn));
        if (extendPastEdge)
        {
            startX -= _control.CharacterWidth / 2;
        }

        var endX = Math.Max(0, _calculator.GetLeftRelativeToColumn((int)(endOffset % bytesPerRow) + 1, column, true) * _control.CharacterWidth);
        var endY = endRow * _control.RowHeight + _control.HeaderHeight;

        var endOffsetVisible = IsOffsetVisibleHorizontally((endOffset + 1) % bytesPerRow is 0 ? endOffset : endOffset + 1);
        extendPastEdge = characterSet.Groupable && IsEndOfGroup(_documentState, endOffset + 1) && endOffsetVisible;
        if (extendPastEdge)
        {
            endX += _control.CharacterWidth / 2;
        }

        return new MarkerPosition(startOffset, startRow, new SharedPoint(startX, startY), endOffset, endRow,
            new SharedPoint(endX, endY));
    }

    private void DrawMarkerAreaSimple(
        ISharedBrush? brush,
        ISharedPen? pen,
        EditorColumn column,
        Marker marker,
        MarkerPosition position)
    {
        // TODO: fix funky rectangle borders, introduce a 'render flags' option in IRenderContext to determine how borders behave :)!
        var columnWidth = _calculator.GetVisibleColumnWidth(column) * _control.CharacterWidth;
        var aliasOffset = GetLineAntiAliasOffset(pen);
        var characterSet = _calculator.GetCharacterSetForColumn(column);

        var square = position.StartOffset % _documentState.Configuration.BytesPerRow is 0 &&
                     (position.EndOffset + 1) % _documentState.Configuration.BytesPerRow is 0;

        if (position.StartRow == position.EndRow || square)
        {
            // +------------+ 
            // |            |
            // +------------+
            var rect = new SharedRectangle(
                position.Start.X - aliasOffset,
                position.Start.Y - aliasOffset,
                position.End.X - position.Start.X + aliasOffset * 2,
                (position.EndRow - position.StartRow + 1) * _control.RowHeight + aliasOffset * 2);
            _context.DrawRectangle(brush, pen, rect);
        }
        else if (marker.Length <= _documentState.Configuration.BytesPerRow)
        {
            //              +------------+ 
            //              |            |
            //              +------------+
            // +------------+ 
            // |            |
            // +------------+
            var extendPastEdge = characterSet.Groupable ? _control.CharacterWidth / 2 : 0;

            var firstRect = new SharedRectangle(
                position.Start.X - aliasOffset,
                position.Start.Y - aliasOffset,
                columnWidth - position.Start.X + extendPastEdge + aliasOffset * 2,
                _control.RowHeight + aliasOffset * 2);
            _context.DrawRectangle(brush, pen, firstRect);

            if (position.End.X > 0)
            {
                var secondRect = new SharedRectangle(
                    -extendPastEdge - aliasOffset,
                    position.End.Y - aliasOffset,
                    position.End.X + extendPastEdge + aliasOffset * 2,
                    _control.RowHeight + aliasOffset * 2);
                _context.DrawRectangle(brush, pen, secondRect);
            }
        }
    }

    // When not offsetting borders it will take up two half pixels (blurry)
    // TODO: find a more convenient solution for this, because this is terrible
    private static double GetLineAntiAliasOffset(ISharedPen? pen) =>
        pen is not null && pen.Thickness % 2 is not 0 ? .5 : 0;

    private unsafe void DrawMarkerAreaAdvanced(
        ISharedBrush? brush,
        ISharedPen? pen,
        EditorColumn column,
        MarkerPosition position)
    {
        //           +------------+
        //           |            |
        // +---------+    +-------+
        // |              |
        // +--------------+

        var characterSet = _calculator.GetCharacterSetForColumn(column);
        var columnWidth = _calculator.GetVisibleColumnWidth(column) * _control.CharacterWidth;
        var aliasOffset = GetLineAntiAliasOffset(pen);

        var hideLastRow = position.End.X is 0;
        var extendPastEdge = characterSet.Groupable ? _control.CharacterWidth / 2 : 0;

        var pointCount = 3 + (position.StartOffset is 0 || position.End.Y is 0 ? 1 : 2)
            + (position.EndOffset == _documentState.Configuration.BytesPerRow - 1 || hideLastRow ? 1 : 3);
        Span<SharedPoint> points = stackalloc SharedPoint[pointCount];
        var pointIndex = 0;

        if (position.StartOffset is 0 || position.End.Y is 0)
        {

            points[pointIndex++] = new SharedPoint(-extendPastEdge + -aliasOffset, position.Start.Y - aliasOffset);
        }
        else
        {
            points[pointIndex++] = (new SharedPoint(position.Start.X - aliasOffset, position.Start.Y + _control.RowHeight - aliasOffset));
            points[pointIndex++] = new SharedPoint(position.Start.X - aliasOffset, position.Start.Y - aliasOffset);
        }

        points[pointIndex++] = new SharedPoint(columnWidth + extendPastEdge + aliasOffset, position.Start.Y - aliasOffset);

        if (position.EndOffset == _documentState.Configuration.BytesPerRow - 1 || hideLastRow)
        {
            points[pointIndex++] = new SharedPoint(columnWidth + extendPastEdge - aliasOffset, position.End.Y + aliasOffset);
        }
        else
        {
            points[pointIndex++] = new SharedPoint(columnWidth + extendPastEdge + aliasOffset, position.End.Y + aliasOffset);
            points[pointIndex++] = new SharedPoint(position.End.X + aliasOffset, position.End.Y + aliasOffset);
            points[pointIndex++] = new SharedPoint(position.End.X + aliasOffset, position.End.Y + _control.RowHeight + aliasOffset);
        }

        points[pointIndex++] = new SharedPoint(-extendPastEdge + -aliasOffset, position.End.Y + (hideLastRow ? 0 : _control.RowHeight) + aliasOffset);
        points[pointIndex] = new SharedPoint(-extendPastEdge + -aliasOffset, position.Start.Y + _control.RowHeight - aliasOffset);

        _context.DrawPolygon(brush, pen, points);
    }

    private void UpdateMarkerBrushOverrides(Marker marker)
    {
        var rightOffset = _renderState.MarkerForegroundLookup.Length / 2;
        var foreground = marker.Foreground ?? (marker.BehindText ? DetermineTextColor(marker.Background) : null);
        if (foreground is null || _renderState.ColorToBrushCache[foreground.Value] is not { } foregroundBrush)
        {
            return;
        }

        var startOffset = Math.Max(0, marker.Offset - _documentState.Offset);
        var length = Math.Min(_bytesLength - startOffset,
            marker.Length - (marker.Offset < _documentState.Offset ? _documentState.Offset - marker.Offset : 0));
        for (var j = 0; j < length; j++)
        {
            if (marker.Column is MarkerColumn.DataText)
            {
                _renderState.MarkerForegroundLookup[startOffset + j] = foregroundBrush;
                _renderState.MarkerForegroundLookup[rightOffset + startOffset + j] = foregroundBrush;
            }
            else
            {
                var offset = marker.Column is MarkerColumn.Data ? 0 : rightOffset;
                _renderState.MarkerForegroundLookup[offset + startOffset + j] = foregroundBrush;
            }
        }
    }

    private bool ShouldAddMarkerDirtyRect()
    {
        var count = _documentState.Markers.Length;
        if (count != _renderState.PreviousMarkers.Count)
        {
            return true;
        }

        var span = _documentState.Markers.Span;
        for (var i = 0; i < count; i++)
        {
            if (IsDirtyMarker(span[i]))
            {
                return true;
            }
        }

        if (_renderState.InactiveMarker is not null && IsDirtyMarker(_renderState.InactiveMarker))
        {
            return true;
        }

        if (_renderState.ActiveMarker is not null && IsDirtyMarker(_renderState.ActiveMarker))
        {
            return true;
        }

        return false;
    }

    private bool IsDirtyMarker(Marker marker)
    {
        if (_renderState.PreviousMarkers.TryGetValue(marker, out var previousRange))
        {
            return previousRange.Offset != marker.Offset || previousRange.Length != marker.Length;
        }
        else
        {
            return true;
        }
    }

    #endregion

    #region Content
    private unsafe void WriteHexOffsetHeader(ITextBuilder builder)
    {
        const int padZeroCount = 2;

        Span<char> characterBuffer = stackalloc char[20];

        var groupSize = _documentState.Configuration.GroupSize;
        var invisibleGroups = (double)_calculator.HorizontalCharacterOffset / groupSize;

        var visibleCharacters =
            (int)((1 - (invisibleGroups - (int)invisibleGroups)) * (_calculator.LeftCharacterSet.Width * groupSize));
        var firstPartiallyVisible = visibleCharacters > 0;
        var incrementX = (_calculator.LeftCharacterSet.Width * groupSize + 1) * _control.CharacterWidth;
        var totalX = !firstPartiallyVisible ? 0 : (visibleCharacters + 1) * _control.CharacterWidth;

        var startGroup = (int)invisibleGroups;
        var groupCount = _documentState.Configuration.BytesPerRow / groupSize / _calculator.LeftCharacterSet.DataWidth;
        for (var currentGroup = startGroup; currentGroup < groupCount; currentGroup++)
        {
            var firstGroup = currentGroup == startGroup;

            var length = BaseConverter.Convert(currentGroup, _documentState.Configuration.OffsetBase, true, characterBuffer);
            var padZeros = Math.Max(0, padZeroCount - length);

            var x = firstGroup ? 0 : totalX;
            var y = -(_control.CharacterHeight + 2);

            for (var i = 0; i < length + padZeros; i++)
            {
                // Overflow if group size is 1 or first visible area is smaller than 2 characters
                if ((i - 1) % 2 == 1 && (groupSize == 1 || visibleCharacters == 2 && firstGroup) || i == 0)
                {
                    y += _control.CharacterHeight + 2;
                    builder.Next(new SharedPoint(x, y));
                }

                var charIndex = length - (i - padZeros) - 1;
                var @char = charIndex < 0 || charIndex >= length ? '0' : characterBuffer[charIndex];

                builder.Add(_control.HeaderForeground, @char);
            }

            totalX += firstGroup ? 0 : incrementX;
        }
    }

    private void WriteTextHeader(ITextBuilder builder)
    {
        var left = _documentState.Configuration.ColumnsVisible is VisibleColumns.DataText &&
                   _calculator.HorizontalCharacterOffset < _documentState.Configuration.BytesPerRow
            ? (_calculator.GetVisibleColumnWidth(EditorColumn.Left) + SPACING_BETWEEN_COLUMNS) * _control.CharacterWidth
            : 0;
        if (left > _owner.Width)
        {
            return;
        }

        builder.Next(new SharedPoint(Math.Max(0, left), 0));
        builder.Add(_control.HeaderForeground, _control.TextHeader);
    }

    private void WriteContentBytes(ITextBuilder builder)
    {
        var state = _documentState;

        Span<char> characterBuffer = stackalloc char[20];

        var leftCharacterSet = _calculator.LeftCharacterSet;
        var rightCharacterSet = _calculator.RightCharacterSet;

        var leftWidth = _calculator.GetColumnWIdth(EditorColumn.Left);
        var horizontalSpace = Math.Ceiling(_owner.Width / _control.CharacterWidth);
        var verticalSpace = Math.Ceiling(_owner.Height / _control.RowHeight) + _control.RowHeight;
        var leftColumnWidth = _calculator.GetVisibleColumnWidth(EditorColumn.Left);
        var textMiddleOffset = (int)Math.Round(_control.RowHeight / 2d - _control.CharacterHeight / 2d);

        var typeface = builder.Typeface;
        var groupSize = _documentState.Configuration.GroupSize;
        var bytesPerRow = _documentState.Configuration.BytesPerRow;

        // Round content length to full row lengths for padding of final row
        var maxBytesToWrite = _documentState.Configuration.ColumnsVisible is not VisibleColumns.DataText
            ? bytesPerRow
            : bytesPerRow * 2;

        var horizontalOffset = _calculator.HorizontalOffset;

        for (var row = 0; row < verticalSpace; row++)
        {
            var y = row * _control.RowHeight + _control.HeaderHeight + textMiddleOffset;
            var byteColumn = 0;
            var visualCol = 0;
            var bytesWritten = 0;

            var horizontalCharacterOffset = _calculator.HorizontalCharacterOffset;
            builder.Next(new SharedPoint(0, y));

            while (visualCol < horizontalSpace &&
                   ++bytesWritten + _calculator.HorizontalCharacterOffset <= maxBytesToWrite)
            {
                var column = visualCol < leftWidth - horizontalOffset ? EditorColumn.Left : EditorColumn.Right;
                var characterSet = column is EditorColumn.Left
                    ? leftCharacterSet
                    : rightCharacterSet;
                
                var columnIndex = (byteColumn + horizontalCharacterOffset) % bytesPerRow;
                var byteIndex = row * bytesPerRow + columnIndex;
                
                if (characterSet is not null)
                {
                    visualCol += WriteSingleContentByte(
                        builder,
                        typeface,
                        characterSet,
                        characterBuffer,
                        column,
                        byteIndex,
                        columnIndex,

                        // Performance reasons
                        groupSize,
                        bytesPerRow);

                    byteColumn += characterSet.DataWidth;
                }

                if (columnIndex >= bytesPerRow - 1)
                {
                    if (column is EditorColumn.Right)
                    {
                        break;
                    }

                    horizontalCharacterOffset = 0;
                    visualCol = leftColumnWidth + SPACING_BETWEEN_COLUMNS + 1;
                    byteColumn = 0;
                }
            }
        }
    }

    private int WriteSingleContentByte(
        ITextBuilder builder, 
        IGlyphTypeface typeface, 
        CharacterSet characterSet,
        Span<char> characterBuffer, 
        EditorColumn column,
        int byteIndex, 
        int columnIndex,
        
        // Perf reasons
        int groupSize,
        int bytesPerRow)
    {
        int writtenCharacters;
        if (byteIndex > _bytesLength - 1) // Pad excess bytes with whitespaces
        {
            writtenCharacters = characterSet.Width;
            builder.Whitespace(writtenCharacters);
        }
        else
        {
            var brush = LookupBrushForByte(characterSet, column, byteIndex, columnIndex, groupSize);
            var bytes = _bytes.AsSpan(byteIndex, Math.Min(characterSet.DataWidth, _bytes.Length - (byteIndex + characterSet.DataWidth)));
            writtenCharacters = WriteByteToTextBuilder(builder, typeface, brush, characterSet, characterBuffer, bytes);
        }

        // Add whitespace between characters
        if (column is EditorColumn.Left && (columnIndex + characterSet.DataWidth) == bytesPerRow)
        {
            builder.Whitespace(SPACING_BETWEEN_COLUMNS);
            writtenCharacters += SPACING_BETWEEN_COLUMNS;
        }
        else if (characterSet.Groupable && columnIndex % groupSize == groupSize - 1)
        {
            builder.Whitespace();
            writtenCharacters++;
        }

        return writtenCharacters;
    }

    private ISharedBrush LookupBrushForByte(CharacterSet characterSet, EditorColumn column, int byteIndex,
        int columnIndex, int groupSize)
    {
        // Uses an array twice the size of the bytes buffer instead of dictionary, since looking up an array item by index is faster.
        var offset = column is EditorColumn.Left ? 0 : _renderState.MarkerForegroundLookup.Length / 2;
        var brush = _renderState.MarkerForegroundLookup[offset + byteIndex];
        if (brush is not null)
        {
            return brush;
        }

        return characterSet.Groupable && columnIndex / groupSize % 2 == 1
            ? _control.EvenForeground
            : _control.Foreground;
    }

    private static int WriteByteToTextBuilder(
        ITextBuilder builder,
        IGlyphTypeface typeface,
        ISharedBrush brush,
        CharacterSet characterSet,
        Span<char> characterBuffer,
        ReadOnlySpan<byte> bytes)
    {
        var characterCount = characterSet.GetCharacters(bytes, characterBuffer);
        for (var i = 0; i < characterCount; i++)
        {
            var @char = characterBuffer[i];
            if (char.IsControl(@char) || !typeface.TryGetGlyphIndex(@char, out _))
            {
                @char = '.';
            }

            builder.Add(brush, @char);
        }

        return characterSet.Width;
    }
    #endregion

    #region Carets
    private void DrawCarets()
    {
        if (_documentState.Caret.Offset < _documentState.Offset || _documentState.Caret.Offset > _documentState.Offset + _bytesLength)
        {
            return;
        }

        DrawCaret(EditorColumn.Left);

        if (_documentState.Configuration.ColumnsVisible is VisibleColumns.DataText)
        {
            DrawCaret(EditorColumn.Right);
        }

        if (_renderState.CaretUpdated || _renderState.PreviousCaretOffset != _documentState.Caret.Offset)
        {
            AddCaretDirtyRect(_documentState.Caret);
        }


        _renderState.CaretUpdated = false;
        _renderState.PreviousCaretOffset = _documentState.Caret.Offset;
    }

    private void DrawCaret(EditorColumn column)
    {
        var caret = _documentState.Caret;
        var caretColumn = MapFromActiveColumn(caret.Column);
        var characterSet = _calculator.GetCharacterSetForColumn(column);

        if (_context.DirtyRect && _renderState.PreviousCaretOffset != caret.Offset)
        {
            var previousPosition = CalculateCaretPosition(_renderState.PreviousCaretOffset, 0, characterSet, column);
            if (previousPosition.X >= 0 && previousPosition.Y <= _owner.Height)
            {
                _owner.AddDirtyRect(new SharedRectangle(previousPosition.X, previousPosition.Y,
                    characterSet.Width * _control.CharacterWidth, _control.RowHeight), _control.CharacterWidth);
            }
        }

        var drawCaret = _renderState.PreviousCaretOffset != caret.Offset &&
                        (caretColumn == column || !_documentState.Selection.HasValue) ||
                        (caretColumn != column || _renderState.CaretTick) &&
                        (caretColumn == column || !_documentState.Selection.HasValue);

        var position = CalculateCaretPosition(caret.Offset, caret.Nibble, characterSet, column);
        if (position.X < 0 || position.Y > _owner.Height)
        {
            return;
        }

        if (column == caretColumn)
        {
            var topOffset = 0;
            var leftOffset = characterSet.Groupable && IsEndOfGroup(_documentState, caret.Offset) &&
                             _documentState.Selection?.End == caret.Offset
                ? _control.CharacterWidth
                : 1;

            // Move the caret up to the end of last row for visual reasons
            var moveCaretUp = _documentState.Selection?.End == caret.Offset &&
                              caret.Offset % _documentState.Configuration.BytesPerRow is 0;

            if (moveCaretUp)
            {
                topOffset = _control.RowHeight;

                var groupAdjustment = characterSet.Groupable ? _control.CharacterWidth : 1;
                leftOffset -= (_calculator.GetVisibleColumnWidth(column) * _control.CharacterWidth) + groupAdjustment;
            }

            var rect = new SharedRectangle(position.X - leftOffset, position.Y - topOffset, 2,
                _control.RowHeight);

            if (drawCaret)
            {
                _context.DrawRectangle(_control.CaretBackground, null, rect);
            }
        }
        else
        {
            var pen = new SharedPen(_control.CaretBackground, 1, PenStyle.Dashed);
            var aliasOffset = GetLineAntiAliasOffset(pen);
            var rect = new SharedRectangle(position.X + aliasOffset, position.Y + aliasOffset,
                _control.CharacterWidth * characterSet.Width,
                _control.RowHeight - aliasOffset * 2);

            if (drawCaret)
            {
                _context.DrawRectangle(null, pen, rect);
            }
        }
    }
    
    private void AddCaretDirtyRect(Caret caret)
    {
        var position = CalculateCaretPosition(caret.Offset, 0, _calculator.LeftCharacterSet, EditorColumn.Left);
        _control.AddDirtyRect(new SharedRectangle(position.X, position.Y, _calculator.LeftCharacterSet.Width * _control.CharacterWidth, _control.RowHeight),
            _control.CharacterWidth);

        if (_calculator.RightCharacterSet is not null)
        {
            var leftWidth = _calculator.GetVisibleColumnWidth(EditorColumn.Left) * _control.CharacterWidth;

            position = CalculateCaretPosition(caret.Offset, 0, _calculator.LeftCharacterSet, EditorColumn.Right);
            _control.AddDirtyRect(
                new SharedRectangle(leftWidth + position.X, position.Y, _calculator.LeftCharacterSet.Width * _control.CharacterWidth, _control.RowHeight * 2),
                _control.CharacterWidth);
        }
    }

    private SharedPoint CalculateCaretPosition(long offset, long nibble, CharacterSet characterSet, EditorColumn column)
    {
        var relativeOffset = offset - _documentState.Offset;
        var row = relativeOffset / _documentState.Configuration.BytesPerRow;
        var columIndex = (int)(offset % _documentState.Configuration.BytesPerRow);

        var x = (_calculator.GetLeftRelativeToColumn(columIndex, column) +
                Math.Min(characterSet.Width - 1, nibble)) * _control.CharacterWidth;
        var y = row * _control.RowHeight + _control.HeaderHeight;
        if (column is EditorColumn.Right && _calculator.HorizontalCharacterOffset < _documentState.Configuration.BytesPerRow)
        {
            x += (_calculator.GetVisibleColumnWidth(EditorColumn.Left) + SPACING_BETWEEN_COLUMNS) * _control.CharacterWidth;
        }

        return new SharedPoint(x, y);
    }
    #endregion

    #region Utilities
    private static bool IsEndOfGroup(CapturedState state, long offset)
    {
        var column = offset % state.Configuration.BytesPerRow;
        return column % state.Configuration.GroupSize == 0;
    }

    private static Color? DetermineTextColor(Color? background)
    {
        const int threshold = 105;

        if (background?.A is not 255)
        {
            return null;
        }

        var delta = Convert.ToInt32(background.Value.R * 0.299 + background.Value.G * 0.587 +
                                    background.Value.B * 0.114);

        return 255 - delta < threshold ? Color.Black : Color.White;
    }

    private bool IsOffsetVisibleHorizontally(long offset)
    {
        var columnIndex = offset % _documentState.Configuration.BytesPerRow;
        var invisibleColumnIndex = _calculator.HorizontalCharacterOffset % _documentState.Configuration.BytesPerRow;
        return columnIndex > invisibleColumnIndex;
    }

    private EditorColumn MapFromActiveColumn(ActiveColumn column)
    {
        return column switch
        {
            ActiveColumn.Data => _documentState.Configuration.ColumnsVisible is VisibleColumns.Data or VisibleColumns.DataText
                ? EditorColumn.Left
                : EditorColumn.Right,
            ActiveColumn.Text => _documentState.Configuration.ColumnsVisible is VisibleColumns.DataText
                ? EditorColumn.Right
                : default!,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
    }

    #endregion

    private record struct MarkerPosition(long StartOffset, long StartRow, SharedPoint Start, long EndOffset,
        long EndRow, SharedPoint End);
}
