using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Modifications;
using HexControl.Core.Characters;
using HexControl.Core.Events;
using HexControl.Core.Observable;
using JetBrains.Annotations;

namespace HexControl.Core;

[PublicAPI]
public enum NewCaretLocation
{
    Current,
    SelectionEnd,
    SelectionStart
}

[PublicAPI]
public class Caret : IEquatable<Caret>
{
    public Caret(long offset, int nibble, ColumnSide column)
    {
        Offset = offset;
        Nibble = nibble;
        Column = column;
    }

    public long Offset { get; }
    public int Nibble { get; }
    public ColumnSide Column { get; }

    public bool Equals(Caret? other) => other is not null && other.Offset == Offset && other.Nibble == Nibble &&
                                        other.Column == Column;

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Caret other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Offset, Nibble, Column);
}

[PublicAPI]
public class CaretChangedEventArgs : EventArgs
{
    internal CaretChangedEventArgs(Caret oldCaret, Caret newCaret, bool scrollToCaret)
    {
        OldCaret = oldCaret;
        NewCaret = newCaret;
        ScrollToCaret = scrollToCaret;
    }

    public Caret OldCaret { get; }
    public Caret NewCaret { get; }
    public bool ScrollToCaret { get; }
}

internal record MarkerState(IDocumentMarker Marker, long Offset, long Length);

internal record DocumentState(
    IReadOnlyList<MarkerState> MarkerStates,
    Selection? SelectionState = null,
    Caret? CaretState = null);

[PublicAPI]
public class Document
{
    private readonly List<IDocumentMarker> _markers;
    private readonly Stack<DocumentState> _redoStates;

    private readonly Stack<DocumentState> _undoStates;
    private DocumentConfiguration _configuration;

    private long _offset;

    public Document(BaseBuffer buffer, DocumentConfiguration? configuration = null)
    {
        Buffer = ReplaceBuffer(buffer);

        Configuration = _configuration = configuration ?? new DocumentConfiguration();

        _undoStates = new Stack<DocumentState>();
        _redoStates = new Stack<DocumentState>();

        _markers = new List<IDocumentMarker>();
        Caret = new Caret(0, 0, ColumnSide.Left);
    }

    public StaticMarkerProvider? StaticMarkerProvider { get; set; }

    public Caret Caret { get; private set; }

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            _configuration.PropertyChanged -= OnPropertyChanged;
            _configuration = value ?? throw new ArgumentNullException(nameof(value));
            _configuration.PropertyChanged += OnPropertyChanged;
            OnPropertyChanged(this, new PropertyChangedEventArgs());
        }
    }

    public BaseBuffer Buffer { get; private set; }

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
            OnOffsetChanged(oldOffset, newOffset);
        }
    }

    public Selection? Selection { get; private set; }

    // Maximum possible offset taking into account 'Configuration.BytesPerRow'
    public long MaximumOffset => FloorOffsetToNearestRow(Length);

    public event EventHandler<PropertyChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CaretChangedEventArgs>? CaretChanged;
    public event EventHandler<OffsetChangedEventArgs>? OffsetChanged;
    public event EventHandler<LengthChangedEventArgs>? LengthChanged;
    public event EventHandler<EventArgs>? MarkersChanged;
    public event EventHandler<EventArgs>? Saved;

    private void BufferOnLengthChanged(object? sender, LengthChangedEventArgs e)
    {
        OnLengthChanged(e.OldLength, e.NewLength);
    }

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

    private void BufferOnSaved(object? sender, EventArgs e)
    {
        _undoStates.Clear();
        _redoStates.Clear();

        OnSaved();
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

        if (state.CaretState is not null)
        {
            Caret = state.CaretState;
        }
    }

    private long FloorOffsetToNearestRow(long number)
    {
        var bytesPerRow = Configuration.BytesPerRow;
        return Math.Max(0,
            (int)(Math.Ceiling(number / (double)bytesPerRow) * bytesPerRow) - bytesPerRow);
    }

    protected void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Reset the horizontal offset (scroll) when changing the columns or character sets
        if (e.Property is nameof(Configuration.ColumnsVisible) or nameof(Configuration.LeftCharacterSet)
            or nameof(Configuration.RightCharacterSet))
        {
            HorizontalOffset = 0;
        }

        ConfigurationChanged?.Invoke(sender, e);
    }

    public static Document FromFile(string fileName, FileOpenMode openMode = FileOpenMode.ReadWrite,
        DocumentConfiguration? configuration = null) =>
        new(new FileBuffer(fileName, openMode), configuration);

    public static Document FromBytes(byte[] bytes, bool readOnly = false,
        DocumentConfiguration? configuration = null) =>
        new(new MemoryBuffer(bytes, readOnly), configuration);

    public void ChangeCaret(ColumnSide column, long offset, int nibble, bool scrollToCaret = false)
    {
        var newCaret = new Caret(offset, nibble, column);
        if (!ValidateCaret(newCaret))
        {
            return;
        }

        var oldCaret = Caret;
        Caret = newCaret;

        if (!oldCaret.Equals(newCaret))
        {
            OnCaretChanged(oldCaret, Caret, scrollToCaret);
        }
    }

    public void Select(long startOffset, long endOffset, ColumnSide column,
        NewCaretLocation newCaretLocation = NewCaretLocation.Current,
        bool requestCenter = false)
    {
        Select(new Selection(startOffset, endOffset, column), newCaretLocation, requestCenter);
    }

    // requestCenter = request the listener to center the hex viewer around the selection (e.g. useful when highlighting a find result).
    public void Select(Selection? newArea, NewCaretLocation newCaretLocation = NewCaretLocation.Current,
        bool requestCenter = false)
    {
        var oldArea = Selection;

        // TODO: offset validation
        if (newArea?.Equals(oldArea) == true)
        {
            return;
        }

        Selection = newArea;

        if (newArea is not null && newCaretLocation is not NewCaretLocation.Current)
        {
            Caret = new Caret(newCaretLocation is NewCaretLocation.SelectionStart ? newArea.Start : newArea.End, 0,
                newArea.Column);
        }

        OnSelectionChanged(oldArea, newArea, requestCenter);
    }

    public Guid AddMarker(Guid id, IDocumentMarker marker)
    {
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

    private bool ValidateCaret(Caret caret)
    {
        if (caret.Offset < 0 || caret.Offset > Length)
        {
            throw new ArgumentException("Caret offset must be between zero and Length of the document.");
        }

        if (caret.Nibble < 0 || caret.Nibble >= GetCharacterSetForColumn(caret.Column).Width)
        {
            throw new ArgumentException("Caret nibble must be between zero and Width of the column's character set.");
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

    public BaseBuffer ReplaceBuffer(BaseBuffer newBuffer)
    {
        var oldBuffer = Buffer;

        if (newBuffer.IsModified)
        {
            throw new ArgumentException("Buffer can only be replaced by a fresh buffer.", nameof(newBuffer));
        }

        // Old buffer is null when called from the constructor
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (oldBuffer is not null)
        {
            if (newBuffer.Length != Buffer.Length)
            {
                throw new ArgumentException("Buffer can only be replaced by a buffer of equal size.",
                    nameof(newBuffer));
            }

            oldBuffer.LengthChanged -= BufferOnLengthChanged;
            oldBuffer.Modified -= BufferOnModified;
            oldBuffer.Saved -= BufferOnSaved;
        }

        newBuffer.LengthChanged += BufferOnLengthChanged;
        newBuffer.Modified += BufferOnModified;
        newBuffer.Saved += BufferOnSaved;
        return Buffer = newBuffer;
    }

    protected virtual void OnCaretChanged(Caret oldCaret, Caret newCaret, bool scrollToCaret)
    {
        CaretChanged?.Invoke(this, new CaretChangedEventArgs(oldCaret, newCaret, scrollToCaret));
    }

    protected virtual void OnSelectionChanged(Selection? oldArea, Selection? newArea, bool requestCenter)
    {
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(oldArea, newArea, requestCenter));
    }

    protected virtual void OnOffsetChanged(long oldOffset, long newOffset)
    {
        OffsetChanged?.Invoke(this, new OffsetChangedEventArgs(oldOffset, newOffset));
    }

    protected virtual void OnLengthChanged(long oldLength, long newLength)
    {
        LengthChanged?.Invoke(this, new LengthChangedEventArgs(oldLength, newLength));
    }

    protected virtual void OnMarkersChanged()
    {
        MarkersChanged?.Invoke(this, EventArgs.Empty);
    }

    protected virtual void OnSaved()
    {
        Saved?.Invoke(this, EventArgs.Empty);
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

        if (deleteOffset >= Caret.Offset)
        {
            return new DocumentState(markerStates);
        }

        var caretState = new Caret(Caret.Offset, Caret.Nibble, Caret.Column);
        Caret = new Caret(Caret.Offset, 0, Caret.Column);
        return new DocumentState(markerStates, CaretState: caretState);
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

        var caretState = new Caret(Caret.Offset, Caret.Nibble, Caret.Column);

        // Don't move caret to the right for single byte inserts, this usually means a user is typing
        if (insertBytes.Length >= 2)
        {
            Caret = new Caret(insertOffset + insertBytes.Length, 0, Caret.Column);
        }

        return new DocumentState(markerStates, CaretState: caretState);
    }

    private DocumentState ApplyWriteModification(long writeOffset, byte[] writeBytes)
    {
        var caretState = new Caret(Caret.Offset, Caret.Nibble, Caret.Column);
        return new DocumentState(Array.Empty<MarkerState>(), CaretState: caretState);
    }
}