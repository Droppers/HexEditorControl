using HexControl.Buffers;
using HexControl.Buffers.Events;
using HexControl.Buffers.Modifications;
using HexControl.Framework.Clipboard;
using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents.Events;
using JetBrains.Annotations;
using System.Buffers;
using System.Runtime.InteropServices;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public class Document
{
    private const int CLIPBOARD_LIMIT = 1024 * 1024 * 32;

    private static readonly IReadOnlyDictionary<Guid, MarkerState> EmptyMarkerStateDictionary =
        new Dictionary<Guid, MarkerState>();


    private readonly Stack<(DocumentState Undo, DocumentState Redo)> _redoStates;
    private readonly Stack<(DocumentState Undo, DocumentState Redo)> _undoStates;

    private DocumentConfiguration _configuration;
    private long _offset;

    private IDocumentMarker[] _capturedMarkers;
    private CapturedState? _capturedState;
    private int _markersVersion;
    private int _capturedMarkersVersion = -1;

    private Caret _caret;
    private Selection? _selection;

    public Document(ByteBuffer buffer, DocumentConfiguration? configuration = null)
    {
        Buffer = ReplaceBuffer(buffer);

        Configuration = _configuration = configuration ?? new DocumentConfiguration();

        _undoStates = new Stack<(DocumentState Undo, DocumentState Redo)>();
        _redoStates = new Stack<(DocumentState Undo, DocumentState Redo)>();

        InternalMarkers = new List<IDocumentMarker>();
        _capturedMarkers = new IDocumentMarker[1000];

        Caret = new Caret(0, 0, ActiveColumn.Hex);
    }

    public DocumentConfiguration Configuration
    {
        get => _configuration;
        set
        {
            if (value == _configuration)
            {
                return;
            }

            var oldConfiguration = _configuration;
            _configuration = value;
            var changes = _configuration.DetectChanges(oldConfiguration);
            OnConfigurationChanged(oldConfiguration, value, changes);
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
            var newOffset = (long)Math.Floor(value / (double)Configuration.BytesPerRow) * Configuration.BytesPerRow;
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

        public Caret Caret
    {
        get => _caret;
        set
        {
            if (!IsVisibleColumn(value.Column, out var visibleColumn))
            {
                _caret = value with
                {
                    Column = visibleColumn
                };
            }
            else
            {
                _caret = value;
            }
        }
    }

    public Selection? Selection
    {
        get => _selection;
        set
        {
            if (value is { } val && !IsVisibleColumn(val.Column, out var visibleColumn))
            {
                _selection = val with
                {
                    Column = visibleColumn
                };
            }
            else
            {
                _selection = value;
            }
        }
    }

    // Maximum possible offset taking into account 'Configuration.BytesPerRow'
    public long MaximumOffset =>
        (long)Math.Floor(Length / (double)Configuration.BytesPerRow) * Configuration.BytesPerRow
        - (Length % Configuration.BytesPerRow is 0 ? Configuration.BytesPerRow : 0);

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<CaretChangedEventArgs>? CaretChanged;
    public event EventHandler<OffsetChangedEventArgs>? OffsetChanged;
    public event EventHandler<LengthChangedEventArgs>? LengthChanged;
    public event EventHandler<EventArgs>? MarkersChanged;
    public event EventHandler<EventArgs>? Saved;
    public event EventHandler<ModifiedEventArgs>? Modified;

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
                    ApplyDocumentState(undoState.Undo);
                    break;
                case ModificationSource.Redo:
                    var redoState = _redoStates.Pop();
                    _undoStates.Push(redoState);
                    ApplyDocumentState(redoState.Redo);
                    break;
                case ModificationSource.User:
                    var (newUndoState, newRedoState) = modification switch
                    {
                        DeleteModification(var offset, var length) => ApplyDeleteModification(offset, length),
                        InsertModification(var offset, var bytes) => ApplyInsertModification(offset, bytes),
                        WriteModification(var offset, var bytes) => ApplyWriteModification(offset, bytes),
                        _ => throw new InvalidOperationException(
                            $"Modification with type '{modification.GetType().Name}' is not yet supported.")
                    };
                    _undoStates.Push((newUndoState, newRedoState));
                    _redoStates.Clear();
                    break;
            }
        }

        OnModified(e.Modifications, e.Source);
    }

    private void BufferOnSaved(object? sender, EventArgs e)
    {
        _undoStates.Clear();
        _redoStates.Clear();

        OnSaved();
    }

    private void ApplyDocumentState(DocumentState state)
    {
        var count = InternalMarkers.Count;
        var span = CollectionsMarshal.AsSpan(InternalMarkers);
        for (var i = 0; i < count; i++)
        {
            var marker = span[i];
            if (!state.MarkerStates.TryGetValue(marker.Id, out var markerState))
            {
                continue;
            }

            ChangeMarkerOffsetAndLength(marker, markerState.Offset, markerState.Length);
            break;
        }

        Selection = state.Selection;

        if (state.Caret is { } caretState)
        {
            Caret = caretState;
        }
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

    public void ChangeCaret(ActiveColumn column, long offset, int nibble, bool scrollToCaret = false)
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

    public void Select(long startOffset, long endOffset, ActiveColumn column,
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

    #region Clipboard operations
    public async Task<bool> TryCopyAsync(CancellationToken cancellationToken = default)
    {
        if (Selection.HasValue)
        {
            return await TryCopyAsync(Selection.Value.Start, Selection.Value.Length, cancellationToken);
        }

        return false;
    }

    public async Task<bool> TryCopyAsync(long offset, long length, CancellationToken cancellationToken = default)
    {
        return await TryCopyAsync(offset, length, null, cancellationToken);
    }

    public async Task<bool> TryCopyAsync(long offset, long length, ActiveColumn? column, CancellationToken cancellationToken = default)
    {
        column ??= Caret.Column;

        if (length > CLIPBOARD_LIMIT)
        {
            return false;
        }

        var readBuffer = ArrayPool<byte>.Shared.Rent((int)length);

        try
        {
            await Buffer.ReadAsync(readBuffer.AsMemory(0, (int)length), offset, null, cancellationToken);

            var characterSet = GetCharacterSet(column.Value);
            if (characterSet is IStringConvertible convertible)
            {
                var value = convertible.ToString(readBuffer.AsSpan(0, (int)length), new FormatInfo(offset, Configuration));
                if (string.IsNullOrEmpty(value))
                {
                    return false;
                }

                return await Clipboard.Instance.TrySetAsync(value, cancellationToken);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }

        return false;
    }

    public async Task<bool> TryPasteAsync(CancellationToken cancellationToken = default)
    {
        if (Selection.HasValue)
        {
            return await TryPasteAsync(Selection.Value.Start, Selection.Value.Length, cancellationToken);
        }

        return await TryPasteAsync(Caret.Offset, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, CancellationToken cancellationToken = default)
    {
        return await TryPasteAsync(offset, null, null, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, ActiveColumn? column, CancellationToken cancellationToken = default)
    {
        return await TryPasteAsync(offset, null, column, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, long? length, CancellationToken cancellationToken = default)
    {
        return await TryPasteAsync(offset, length, null, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, long? length, ActiveColumn? column, CancellationToken cancellationToken = default)
    {
        column ??= Caret.Column;

        var (success, content) = await Clipboard.Instance.TryReadAsync(cancellationToken);
        if (!success || string.IsNullOrEmpty(content))
        {
            return false;
        }

        if (content.Length > CLIPBOARD_LIMIT)
        {
            return false;
        }

        var tempBuffer = ArrayPool<byte>.Shared.Rent(content.Length);

        try
        {
            var characterSet = GetCharacterSet(column.Value);
            if (characterSet is IStringParsable parsable)
            {
                if (parsable.TryParse(content, tempBuffer.AsSpan(), out var parsedLength))
                {
                    var writeBuffer = new byte[parsedLength];
                    tempBuffer.AsSpan(0, parsedLength).CopyTo(writeBuffer.AsSpan());

                    if (length.HasValue)
                    {
                        await Buffer.ReplaceAsync(offset, length.Value, writeBuffer);
                    }
                    else
                    {
                        await Buffer.InsertAsync(offset, writeBuffer);
                    }

                    return true;
                }
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tempBuffer);
        }

        return false;
    }
    #endregion

    private CharacterSet GetCharacterSet(ActiveColumn column)
    {
        return column switch
        {
            ActiveColumn.Hex => Configuration.HexCharacterSet,
            ActiveColumn.Text => Configuration.TextCharacterSet,
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

    private CharacterSet GetCharacterSetForColumn(ActiveColumn column)
    {
        return column switch
        {
            ActiveColumn.Hex => Configuration.HexCharacterSet,
            ActiveColumn.Text => Configuration.TextCharacterSet,
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

    #region Events
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

    protected virtual void OnConfigurationChanged(DocumentConfiguration oldConfiguration,
        DocumentConfiguration newConfiguration, IEnumerable<string> changes)
    {
        var changesArray = changes.ToArray();

        // Reset the horizontal offset (scroll) when changing the columns or character sets
        if (changesArray.Any(p => p is nameof(Configuration.ColumnsVisible) or nameof(Configuration.HexCharacterSet)
                or nameof(Configuration.TextCharacterSet)))
        {
            HorizontalOffset = 0;
        }

        // Ensure that invisible columns cannot be active
        if (changesArray.Any(p => p is nameof(Configuration.ColumnsVisible)))
        {
            if (Selection is { } selection)
            {
                Selection = selection with { };
            }

            Caret = Caret with { };
        }

        ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(oldConfiguration, newConfiguration, changesArray));
    }

    protected virtual void OnModified(IReadOnlyList<BufferModification> modifications, ModificationSource source)
    {
        Modified?.Invoke(this, new ModifiedEventArgs(modifications, source));
    }
    #endregion

    private bool IsVisibleColumn(ActiveColumn column, out ActiveColumn visibleColumn)
    {
        if (Configuration.ColumnsVisible is VisibleColumns.HexText)
        {
            visibleColumn = column;
            return true;
        }

        visibleColumn = column switch
        {
            ActiveColumn.Hex when Configuration.ColumnsVisible is VisibleColumns.Text => ActiveColumn.Text,
            ActiveColumn.Text when Configuration.ColumnsVisible is VisibleColumns.Hex => ActiveColumn.Hex,
            _ => column
        };
        return false;
    }

    private (DocumentState Undo, DocumentState Redo) ApplyDeleteModification(long offset, long length)
    {
        var undoMarkerStates = new Dictionary<Guid, MarkerState>();
        var redoMarkerStates = new Dictionary<Guid, MarkerState>();

        for (var i = 0; i < InternalMarkers.Count; i++)
        {
            var marker = InternalMarkers[i];
            var (newOffset, newLength) = DeleteFromRange(marker.Offset, marker.Length, offset, length);
            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                undoMarkerStates.Add(marker.Id, new MarkerState(marker.Offset, marker.Length)); 
                redoMarkerStates.Add(marker.Id, new MarkerState(newOffset, newLength));
                ChangeMarkerOffsetAndLength(marker, newOffset, newLength);
            }
        }

        Caret? undoCaret = Caret with { };
        Caret? redoCaret = Caret with { };
        if (offset < Caret.Offset)
        {
            redoCaret = Caret = Caret with
            {
                Offset = Caret.Offset - length,
                Nibble = 0
            };
        }

        var undoSelection = Selection.HasValue ? Selection with { } : null;
        Selection? redoSelection = null;
        if (Selection is { } selection)
        {
            var (newOffset, newLength) =
                DeleteFromRange(selection.Start, selection.Length, offset, length);
            undoSelection = Selection;
            Selection = redoSelection = newLength <= 0 ? null : selection with
            {
                Start = newOffset,
                Length = newLength
            };
        }

        var undoState = new DocumentState(undoMarkerStates, undoSelection, undoCaret);
        var redoState = new DocumentState(redoMarkerStates, redoSelection, redoCaret);

        return (undoState, redoState);
    }

    private (DocumentState Undo, DocumentState Redo) ApplyInsertModification(long offset, byte[] bytes)
    {
        var undoMarkerStates = new Dictionary<Guid, MarkerState>();
        var redoMarkerStates = new Dictionary<Guid, MarkerState>();

        for (var i = 0; i < InternalMarkers.Count; i++)
        {
            var marker = InternalMarkers[i];

            var (newOffset, newLength) =
                InsertIntoRange(marker.Offset, marker.Length, offset, bytes.Length);
            if (newOffset != marker.Offset || newLength != marker.Length)
            {
                undoMarkerStates.Add(marker.Id, new MarkerState(marker.Offset, marker.Length));
                redoMarkerStates.Add(marker.Id, new MarkerState(newOffset, newLength));
                ChangeMarkerOffsetAndLength(marker, newOffset, newLength);
            }
        }

        var undoCaret = Caret with { };
        var redoCaret = undoCaret;

        // Don't move caret to the right for single byte inserts, this usually means a user is typing
        if (bytes.Length >= 2)
        {
            Caret = redoCaret = Caret with { Offset = offset + bytes.Length, Nibble = 0 };
        }

        var undoSelection = Selection.HasValue ? Selection with { } : null;
        Selection? redoSelection = null;
        if (Selection is { } selection)
        {
            var (newOffset, newLength) =
                InsertIntoRange(selection.Start, selection.Length, offset, bytes.Length);
            undoSelection = Selection;
            Selection = redoSelection = selection with
            {
                Start = newOffset,
                Length = newLength
            };
        }

        var undoState = new DocumentState(undoMarkerStates, undoSelection, undoCaret);
        var redoState = new DocumentState(redoMarkerStates, redoSelection, redoCaret);
        return (undoState, redoState);
    }

    private (DocumentState Undo, DocumentState Redo) ApplyWriteModification(long offset, byte[] bytes)
    {
        var selectionState = Selection.HasValue ? Selection with { } : null;
        var caretState = new Caret(Caret.Offset, Caret.Nibble, Caret.Column);
        var state = new DocumentState(EmptyMarkerStateDictionary, selectionState, caretState);
        return (state, state);
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