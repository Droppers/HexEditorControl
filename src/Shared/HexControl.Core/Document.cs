using System.Drawing;
using HexControl.Core.Buffers;
using HexControl.Core.Characters;
using HexControl.Core.Events;

namespace HexControl.Core;

public class Cursor
{
    public Cursor(long offset, int nibble, ColumnSide column)
    {
        Offset = offset;
        Nibble = nibble;
        Column = column;
    }

    public long Offset { get; }
    public int Nibble { get; }
    public ColumnSide Column { get; }
}

public class CursorChangedEventArgs : EventArgs
{
    public CursorChangedEventArgs(Cursor oldCursor, Cursor newCursor, bool scrollToCursor)
    {
        OldCursor = oldCursor;
        NewCursor = newCursor;
        ScrollToCursor = scrollToCursor;
    }

    public Cursor OldCursor { get; }
    public Cursor NewCursor { get; }
    public bool ScrollToCursor { get; }
}

internal record MarkerState(IDocumentMarker Marker, long Offset, long Length);

internal record DocumentState(
    IReadOnlyList<MarkerState> MarkerStates,
    Selection? SelectionState = null,
    Cursor? CursorState = null);

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
public class Document
{
    private readonly List<IDocumentMarker> _markers;
    private readonly Stack<DocumentState> _redoStates;

    private readonly Stack<DocumentState> _undoStates;
    private DocumentConfiguration _configuration;

    private long _offset;

    public Document(BaseBuffer buffer, DocumentConfiguration? configuration = null)
    {
        Buffer = buffer;
        Buffer.Modified += BufferOnModified;

        Configuration = _configuration = configuration ?? new DocumentConfiguration();

        _undoStates = new Stack<DocumentState>();
        _redoStates = new Stack<DocumentState>();

        _markers = new List<IDocumentMarker>();
        Cursor = new Cursor(0, 0, ColumnSide.Left);
    }

    public Cursor Cursor { get; private set; }

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _configuration.ConfigurationChanged -= OnPropertyChanged;
            _configuration = value ?? throw new ArgumentNullException(nameof(value));
            _configuration.ConfigurationChanged += OnPropertyChanged;
            OnPropertyChanged(this, new ConfigurationChangedEventArgs());
        }
    }

    public BaseBuffer Buffer { get; }

    public IReadOnlyList<IDocumentMarker> Markers => _markers;

    public long Length => Buffer.Length;
    public long OriginalLength => Buffer.OriginalLength;

    public int HorizontalOffset { get; internal set; }

    public long Offset
    {
        get => _offset;
        set
        {
            var newOffset = Math.Min(FloorOffsetToNearestRow(value), MaximumOffset);
            var oldOffset = _offset;
            if (oldOffset == newOffset)
            {
                return;
            }

            _offset = newOffset;
            OffsetChanged?.Invoke(this, new OffsetChangedEventArgs(oldOffset, newOffset));
        }
    }

    public Selection? Selection { get; private set; }

    // Maximum possible offset taking into account 'Configuration.BytesPerRow'
    public long MaximumOffset
    {
        get
        {
            var floored = FloorOffsetToNearestRow(Buffer.Length);
            return Math.Max(0, floored - (floored == Length ? Configuration.BytesPerRow : 0));
        }
    }

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CursorChangedEventArgs>? CursorChanged;
    public event EventHandler<OffsetChangedEventArgs>? OffsetChanged;
    public event EventHandler<EventArgs>? MarkersChanged;

    private void BufferOnModified(object? sender, ModifiedEventArgs e)
    {
        switch (e.Source)
        {
            case ModificationSource.Undo:
                var undoState = _undoStates.Pop();
                _redoStates.Push(undoState);
                ApplyDocumentState(undoState);
                break;
            case ModificationSource.Redo:
                var redoState = _redoStates.Pop();
                _undoStates.Push(redoState);
                ApplyDocumentState(redoState);
                break;
            case ModificationSource.User:
                var userState = e.Modification switch
                {
                    DeleteModification(var offset, var length) => ApplyDeleteModification(offset, length),
                    InsertModification(var offset, var bytes) => ApplyInsertModification(offset, bytes),
                    WriteModification(var offset, var bytes) => ApplyWriteModification(offset, bytes),
                    _ => throw new InvalidOperationException(
                        $"Modification with type '{e.Modification.GetType().Name}' is not yet supported.")
                };
                _undoStates.Push(userState);
                _redoStates.Clear();
                break;
        }
    }

    private void ApplyDocumentState(DocumentState state)
    {
        for (var i = 0; i < _markers.Count; i++)
        {
            var marker = _markers[i];
            for (var j = 0; j < state.MarkerStates.Count; j++)
            {
                var markerState = state.MarkerStates[j];
                if (marker.Id != markerState.Marker.Id)
                {
                    continue;
                }

                marker.Offset = markerState.Offset;
                marker.Length = markerState.Length;
                break;
            }
        }

        if (state.CursorState is not null)
        {
            Cursor = state.CursorState;
        }
    }

    private long FloorOffsetToNearestRow(long number) =>
        Math.Max(0, number - number % Configuration.BytesPerRow);

    protected void OnPropertyChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        // Reset the horizontal offset (scroll) when changing the columns or character sets
        if (e.Property is nameof(Configuration.ColumnsVisible) or nameof(Configuration.LeftCharacterSet)
            or nameof(Configuration.RightCharacterSet))
        {
            HorizontalOffset = 0;
        }

        ConfigurationChanged?.Invoke(sender, e);
    }

    public static Document FromFile(string fileName, DocumentConfiguration? configuration = null) =>
        new(new FileBuffer(fileName), configuration);

    // TODO: implement
    public static Document FromBuffer(byte[] bytes, DocumentConfiguration? configuration = null) =>
        new(new FileBuffer(""), configuration);

    public void ChangeCursor(ColumnSide column, long offset, int nibble, bool scrollToCursor = false)
    {
        var value = new Cursor(offset, nibble, column);
        if (!ValidateCursor(value))
        {
            return;
        }

        var oldCursor = Cursor;
        Cursor = value;

        OnCursorChanged(oldCursor, Cursor, scrollToCursor);
    }

    public void Select(long startOffset, long endOffset, ColumnSide column, bool moveCursor = true,
        bool requestCenter = false)
    {
        Select(new Selection(startOffset, endOffset, column), requestCenter);
    }

    // requestCenter = request the listener to center the hex viewer around the selection (e.g. useful when highlighting a find result).
    public void Select(Selection? newArea, bool moveCursor = true, bool requestCenter = false)
    {
        var oldArea = Selection;

        // TODO: offset validation
        if (newArea?.Equals(oldArea) == true)
        {
            return;
        }

        Selection = newArea;

        if (newArea is not null && moveCursor)
        {
            Cursor = new Cursor(newArea.End, 0, newArea.Column);
        }

        OnSelectionChanged(oldArea, newArea, requestCenter);
    }

    public Guid AddMarker(Guid id, IDocumentMarker marker)
    {
        for (var i = 0; i < _markers.Count; i++)
        {
            if (_markers[i].Id == id)
            {
                throw new ArgumentException($"A marker with id '{id}' already exists.", nameof(id));
            }
        }

        _markers.Add(marker);
        OnMarkersChanged();

        return id;
    }

    public Guid AddMarker(IDocumentMarker marker) => AddMarker(Guid.NewGuid(), marker);

    public void RemoveMarker(Guid id)
    {
        for (var i = 0; i < _markers.Count; i++)
        {
            if (_markers[i].Id == id)
            {
                OnMarkersChanged();
                return;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(id), $"Marker with id '{id}' does not exist.");
    }

    private bool ValidateCursor(Cursor cursor)
    {
        if (cursor.Offset < 0 || cursor.Offset > Length)
        {
            throw new ArgumentException("Cursor offset must be between zero and Length of the document.");
        }

        if (cursor.Nibble < 0 || cursor.Nibble >= GetCharacterSetForColumn(cursor.Column).Width)
        {
            throw new ArgumentException("Cursor nibble must be between zero and Width of the column's character set.");
        }

        return true;
    }

    private CharacterSet GetCharacterSetForColumn(ColumnSide column)
    {
        return column switch
        {
            ColumnSide.Left => Configuration.LeftCharacterSet,
            ColumnSide.Right => Configuration.RightCharacterSet,
            _ => throw new ArgumentOutOfRangeException(nameof(column))
        };
    }

    public void Deselect()
    {
        Select(null);
    }

    protected virtual void OnCursorChanged(Cursor oldCursor, Cursor newCursor, bool scrollToCursor)
    {
        CursorChanged?.Invoke(this, new CursorChangedEventArgs(oldCursor, newCursor, scrollToCursor));
    }

    protected virtual void OnSelectionChanged(Selection? oldArea, Selection? newArea, bool requestCenter)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldArea, newArea, requestCenter));
    }

    protected virtual void OnOffsetChanged(OffsetChangedEventArgs e)
    {
        OffsetChanged?.Invoke(this, e);
    }

    protected virtual void OnMarkersChanged()
    {
        MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    private DocumentState ApplyDeleteModification(long deleteOffset, long deleteLength)
    {
        var markerStates = new List<MarkerState>();

        var deleteEnd = deleteOffset + deleteLength;
        for (var i = 0; i < _markers.Count; i++)
        {
            var marker = _markers[i];
            var markerOffset = marker.Offset;
            var markerEnd = marker.Offset + marker.Length;

            var newOffset = marker.Offset;
            var newLength = marker.Length;

            if (deleteOffset < markerOffset && deleteEnd > markerEnd)
            {
                newOffset -= markerOffset - deleteOffset;
                newLength = 0;
            }

            if (deleteOffset < markerOffset && deleteEnd < markerOffset)
            {
                newOffset -= deleteLength;
            }

            if (deleteEnd > markerOffset && deleteEnd <= markerEnd && deleteOffset <= markerOffset)
            {
                newOffset = deleteOffset;
                newLength -= deleteLength - (markerOffset - deleteOffset);
            }

            if (deleteEnd > markerEnd && deleteOffset >= markerOffset && deleteOffset < markerEnd)
            {
                newLength -= newLength - (deleteOffset - markerOffset);
            }

            if (deleteOffset > markerOffset && deleteEnd < markerEnd)
            {
                newLength -= deleteLength;
            }

            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                markerStates.Add(new MarkerState(marker, marker.Offset, marker.Length));
                marker.Offset = newOffset;
                marker.Length = newLength;
            }
        }

        if (deleteOffset >= Cursor.Offset)
        {
            return new DocumentState(markerStates);
        }

        var cursorState = new Cursor(Cursor.Offset, Cursor.Nibble, Cursor.Column);
        Cursor = new Cursor(Cursor.Offset, 0, Cursor.Column);
        return new DocumentState(markerStates, CursorState: cursorState);
    }

    private DocumentState ApplyInsertModification(long insertOffset, byte[] insertBytes)
    {
        var markerStates = new List<MarkerState>();

        for (var i = 0; i < _markers.Count; i++)
        {
            var marker = _markers[i];
            var markerOffset = marker.Offset;
            var markerEnd = marker.Offset + marker.Length;
            var newOffset = marker.Offset;
            var newLength = marker.Length;

            if (insertOffset < markerOffset)
            {
                newOffset += insertBytes.Length;
            }
            else if (insertOffset >= markerOffset && insertOffset < markerEnd)
            {
                newLength += insertBytes.Length;
            }

            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                markerStates.Add(new MarkerState(marker, marker.Offset, marker.Length));
                marker.Offset = newOffset;
                marker.Length = newLength;
            }
        }

        var cursorState = new Cursor(Cursor.Offset, Cursor.Nibble, Cursor.Column);

        // Don't move cursor to the right for single byte inserts, this usually means a user is typing
        if (insertBytes.Length >= 2)
        {
            Cursor = new Cursor(insertOffset + insertBytes.Length, 0, Cursor.Column);
        }

        return new DocumentState(markerStates, CursorState: cursorState);
    }

    private DocumentState ApplyWriteModification(long writeOffset, byte[] writeBytes)
    {
        var cursorState = new Cursor(Cursor.Offset, Cursor.Nibble, Cursor.Column);
        return new DocumentState(Array.Empty<MarkerState>(), CursorState: cursorState);
    }
}