using HexControl.Buffers;
using HexControl.Buffers.Events;
using HexControl.Buffers.Modifications;
using HexControl.Framework.Observable;
using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents.Events;
using JetBrains.Annotations;
using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public class Document
{
    private const int CLIPBOARD_LIMIT = 1024 * 1024 * 32;

    private readonly Stack<DocumentState> _redoStates;
    private readonly Stack<DocumentState> _undoStates;

    private DocumentConfiguration _configuration;
    private long _offset;

    private IDocumentMarker[] _capturedMarkers;
    private CapturedState? _capturedState;
    private int _markersVersion;
    private int _capturedMarkersVersion = -1;

    public Document(ByteBuffer buffer, DocumentConfiguration? configuration = null)
    {
        Buffer = ReplaceBuffer(buffer);

        Configuration = _configuration = configuration ?? new DocumentConfiguration();

        _undoStates = new Stack<DocumentState>();
        _redoStates = new Stack<DocumentState>();

        InternalMarkers = new List<IDocumentMarker>();
        _capturedMarkers = new IDocumentMarker[1000];

        Caret = new Caret(0, 0, ColumnSide.Left);
    }

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

    public ByteBuffer Buffer { get; private set; }

    public IReadOnlyList<IDocumentMarker> Markers => InternalMarkers;

    // Used internally for faster access to underlying array entries
    internal List<IDocumentMarker> InternalMarkers { get; }

    public long Length => Buffer.Length;
    public long OriginalLength => Buffer.OriginalLength;

    public int HorizontalOffset { get; internal set; }

    public long Offset
    {
        get => _offset;
        set
        {
            var newOffset = (long)Math.Floor(value / (float)Configuration.BytesPerRow) * Configuration.BytesPerRow;
            newOffset = Math.Max(0, Math.Min(newOffset, MaximumOffset));
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
    public long MaximumOffset
    {
        get
        {
            return (long)Math.Floor(Length / (float)Configuration.BytesPerRow) * Configuration.BytesPerRow;
        }
    }

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
        foreach (var modification in e.Modifications)
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
                    var userState = modification switch
                    {
                        DeleteModification(var offset, var length) => ApplyDeleteModification(offset, length),
                        InsertModification(var offset, var bytes) => ApplyInsertModification(offset, bytes),
                        WriteModification(var offset, var bytes) => ApplyWriteModification(offset, bytes),
                        _ => throw new InvalidOperationException(
                            $"Modification with type '{modification.GetType().Name}' is not yet supported.")
                    };
                    _undoStates.Push(userState);
                    _redoStates.Clear();
                    break;
            }
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
        for (var i = 0; i < InternalMarkers.Count; i++)
        {
            var marker = InternalMarkers[i];
            for (var j = 0; j < state.MarkerStates.Count; j++)
            {
                var markerState = state.MarkerStates[j];
                if (marker.Id != markerState.Marker.Id)
                {
                    continue;
                }

                ChangeMarkerOffsetAndLength(marker, markerState.Offset, markerState.Length);
                break;
            }
        }

        if (state.SelectionState.HasValue)
        {
            Selection = state.SelectionState;
        }

        if (state.CaretState is { } caretState)
        {
            Caret = caretState;
        }
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
        ChangeTracking changeTracking = ChangeTracking.UndoRedo, DocumentConfiguration? configuration = null) =>
        new(new FileBuffer(fileName, openMode, changeTracking), configuration);

    public static Document FromBytes(byte[] bytes, bool readOnly = false,
        ChangeTracking changeTracking = ChangeTracking.UndoRedo,
        DocumentConfiguration? configuration = null) =>
        new(new MemoryBuffer(bytes, readOnly, changeTracking), configuration);

    public void ChangeCaret(long offset, bool scrollToCaret = false)
    {
        ChangeCaret(Caret.Column, offset, 0, scrollToCaret);
    }

    public void ChangeCaret(long offset, int nibble, bool scrollToCaret = false)
    {
        ChangeCaret(Caret.Column, offset, nibble, scrollToCaret);
    }

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
        if (newArea is null && oldArea is null || newArea?.Equals(oldArea) == true)
        {
            return;
        }

        Selection = newArea;

        if (newArea.HasValue && newCaretLocation is not NewCaretLocation.Current)
        {
            Caret = new Caret(
                newCaretLocation is NewCaretLocation.SelectionStart ? newArea.Value.Start : newArea.Value.End, 0,
                newArea.Value.Column);
        }

        OnSelectionChanged(oldArea, newArea, requestCenter);
    }

    public void Deselect()
    {
        Select(null);
    }

    public void AddMarker(IDocumentMarker marker)
    {
        InternalMarkers.Add(marker);
        _markersVersion++;
        OnMarkersChanged();
    }

    public void RemoveMarker(IDocumentMarker marker)
    {
        InternalMarkers.Remove(marker);
        _markersVersion++;
    }

    public async Task CopyAsync(long offset, long length, CancellationToken cancellationToken = default)
    {
        if (length > CLIPBOARD_LIMIT)
        {
            return;
        }

        var readBuffer = ArrayPool<byte>.Shared.Rent((int)length);

        try
        {
            await Buffer.ReadAsync(readBuffer.AsMemory(0, (int)length), offset, null, cancellationToken);

            var characterSet = GetCharacterSet(Caret.Column);
            if (characterSet is IStringConvertible convertible)
            {
                var value = convertible.ToString(readBuffer.AsSpan(0, (int)length), new FormatInfo(offset, Configuration));
                // TODO: write to clipboard
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    public async Task PasteAsync(long offset, long? length = null)
    {
        // TODO: read from clipboard
        var value = "5A9000 03000000 04000000 FFFF0000 B8000000 00000000 40";
        if (value.Length > CLIPBOARD_LIMIT)
        {
            return;
        }

        var tempBuffer = ArrayPool<byte>.Shared.Rent(value.Length);

        try
        {
            var characterSet = GetCharacterSet(Caret.Column);
            if (characterSet is IStringParsable parsable)
            {
                if (parsable.TryParse(value, tempBuffer.AsSpan(), out var parsedLength))
                {
                    var writeBuffer = new byte[parsedLength];
                    tempBuffer.AsSpan(0, parsedLength).CopyTo(writeBuffer.AsSpan());

                    if (length.HasValue)
                    {
                        await Buffer.ReplaceAsync(offset, length.Value, writeBuffer);
                    }
                    else
                    {
                        await Buffer.WriteAsync(offset, writeBuffer);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }
    }

    private CharacterSet GetCharacterSet(ColumnSide column)
    {
        return column switch
        {
            ColumnSide.Left => Configuration.LeftCharacterSet,
            ColumnSide.Right => Configuration.RightCharacterSet,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, null)
        };
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

    internal ByteBuffer ReplaceBuffer(ByteBuffer newBuffer)
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

    private DocumentState ApplyDeleteModification(long offset, long length)
    {
        var markerStates = new List<MarkerState>();

        for (var i = 0; i < InternalMarkers.Count; i++)
        {
            var marker = InternalMarkers[i];
            var (newOffset, newLength) = DeleteFromRange(marker.Offset, marker.Length, offset, length);
            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                markerStates.Add(new MarkerState(marker, marker.Offset, marker.Length));
                ChangeMarkerOffsetAndLength(marker, newOffset, newLength);
            }
        }

        Caret? caretState = null;
        if (offset < Caret.Offset)
        {
            caretState = Caret with { };
            Caret = Caret with {Nibble = 0};
        }

        Selection? selectionState = null;
        if (Selection is { } selection)
        {
            var (newOffset, newLength) =
                DeleteFromRange(selection.Start, selection.Length, offset, length);
            selectionState = Selection;
            Selection = newLength <= 0 ? null : selection with
            {
                Start = newOffset,
                Length = newLength
            };
        }

        return new DocumentState(markerStates, selectionState, caretState);
    }

    private DocumentState ApplyInsertModification(long offset, byte[] bytes)
    {
        var markerStates = new List<MarkerState>();

        for (var i = 0; i < InternalMarkers.Count; i++)
        {
            var marker = InternalMarkers[i];

            var (newOffset, newLength) =
                InsertIntoRange(marker.Offset, marker.Length, offset, bytes.Length);
            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                markerStates.Add(new MarkerState(marker, marker.Offset, marker.Length));
                ChangeMarkerOffsetAndLength(marker, newOffset, newLength);
            }
        }

        var caretState = Caret with { };

        // Don't move caret to the right for single byte inserts, this usually means a user is typing
        if (bytes.Length >= 2)
        {
            Caret = Caret with {Offset = offset + bytes.Length, Nibble = 0};
        }

        Selection? selectionState = null;
        if (Selection is { } selection)
        {
            var (newOffset, newLength) =
                InsertIntoRange(selection.Start, selection.Length, offset, bytes.Length);
            selectionState = Selection;
            Selection = selection with
            {
                Start = newOffset,
                Length = newLength
            };
        }

        return new DocumentState(markerStates, selectionState, caretState);
    }

    private DocumentState ApplyWriteModification(long offset, byte[] bytes)
    {
        var caretState = new Caret(Caret.Offset, Caret.Nibble, Caret.Column);
        return new DocumentState(Array.Empty<MarkerState>(), CaretState: caretState);
    }

    private static (long Offset, long Length) DeleteFromRange(long offset, long length, long deleteOffset,
        long deleteLength)
    {
        var end = offset + length;
        var deleteEnd = deleteOffset + deleteLength;
        var newOffset = offset;
        var newLength = length;

        if (deleteOffset < offset && deleteEnd > end)
        {
            newOffset -= offset - deleteOffset;
            newLength = 0;
        }

        if (deleteOffset < offset && deleteEnd < offset)
        {
            newOffset -= deleteLength;
        }

        if (deleteEnd > offset && deleteEnd <= end && deleteOffset <= offset)
        {
            newOffset = deleteOffset;
            newLength -= deleteLength - (offset - deleteOffset);
        }

        if (deleteEnd > end && deleteOffset >= offset && deleteOffset < end)
        {
            newLength -= newLength - (deleteOffset - offset);
        }

        if (deleteOffset > offset && deleteEnd < end)
        {
            newLength -= deleteLength;
        }

        return (newOffset, newLength);
    }

    private static (long Offset, long Length) InsertIntoRange(long offset, long length, long insertOffset,
        long insertLength)
    {
        var markerEnd = offset + length;
        var newOffset = offset;
        var newLength = length;

        if (insertOffset < offset)
        {
            newOffset += insertLength;
        }
        else if (insertOffset >= offset && insertOffset < markerEnd)
        {
            newLength += insertLength;
        }

        return (newOffset, newLength);
    }

    private void ChangeMarkerOffsetAndLength(IDocumentMarker marker, long newOffset, long newLength)
    {
        if (marker.Offset == newOffset && marker.Length == newLength)
        {
            return;
        }

        _markersVersion++;
        marker.Offset = newOffset;
        marker.Length = newLength;
    }

    internal CapturedState CapturedState => _capturedState ??= CaptureState();

    internal CapturedState CaptureState()
    {
        if (InternalMarkers.Count > _capturedMarkers.Length)
        {
            _capturedMarkers = new IDocumentMarker[InternalMarkers.Count * 2];
        }

        if (_markersVersion != _capturedMarkersVersion)
        {
            var capturedSpan = _capturedMarkers.AsSpan(0, InternalMarkers.Count);
            var span = CollectionsMarshal.AsSpan(InternalMarkers);
            span.CopyTo(capturedSpan);
            _capturedMarkersVersion = _markersVersion;
        }

        var capturedMemory = _capturedMarkers.AsMemory(0, InternalMarkers.Count);
        _capturedState = new CapturedState(
            Offset, 
            Length,
            Selection is { } selection ? selection with { } : null,
            Caret with { },
            capturedMemory,
            Configuration);
        return _capturedState.Value;
    }
}

internal readonly record struct CapturedState(long Offset, long Length, Selection? Selection, Caret Caret, Memory<IDocumentMarker> Markers, DocumentConfiguration Configuration);