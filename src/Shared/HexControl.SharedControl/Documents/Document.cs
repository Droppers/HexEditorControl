using HexControl.Buffers;
using HexControl.Buffers.Events;
using HexControl.Buffers.Modifications;
using HexControl.Framework.Clipboard;
using HexControl.SharedControl.Characters;
using HexControl.SharedControl.Documents.Events;
using JetBrains.Annotations;
using System.Buffers;
using System.Runtime.InteropServices;
using HexControl.Framework.Collections;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public class Document
{
    private const int CLIPBOARD_LIMIT = 1024 * 1024 * 32;

    private static readonly DictionarySlim<Guid, MarkerState> EmptyMarkerStateDictionary = new();

    private readonly Stack<(DocumentState Undo, DocumentState? Redo)> _redoStates;
    private readonly Stack<(DocumentState Undo, DocumentState? Redo)> _undoStates;

    private DocumentConfiguration _configuration;
    private long _offset;

    private Marker[] _capturedMarkers;
    private CapturedState? _capturedState;
    private int _markersVersion;
    private int _capturedMarkersVersion = -1;

    private Caret _caret;
    private Selection? _selection;

    public Document(ByteBuffer buffer, DocumentConfiguration? configuration = null)
    {
        Buffer = ReplaceBuffer(buffer);

        Configuration = _configuration = configuration ?? new DocumentConfiguration();

        _undoStates = new Stack<(DocumentState Undo, DocumentState? Redo)>();
        _redoStates = new Stack<(DocumentState Undo, DocumentState? Redo)>();

        InternalMarkers = new List<Marker>();
        _capturedMarkers = new Marker[1000];

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

    public IReadOnlyList<Marker> Markers => InternalMarkers;

    // Used internally for faster access to underlying array entries
    internal List<Marker> InternalMarkers { get; }

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
        internal set
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
        internal set
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
                    ApplyDocumentState(undoState.Undo);

                    if (Buffer.ChangeTracking is ChangeTracking.UndoRedo)
                    {
                        _redoStates.Push(undoState);
                    }
                    break;
                case ModificationSource.Redo:
                    if (Buffer.ChangeTracking is ChangeTracking.UndoRedo)
                    {
                        var redoState = _redoStates.Pop();
                        _undoStates.Push(redoState);

                        if (redoState.Redo is not null)
                        {
                            ApplyDocumentState(redoState.Redo);
                        }
                    }
                    break;
                case ModificationSource.User:
                    ApplyModification(modification);
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

            ref var markerState = ref state.MarkerStates.GetOrAddValueRef(marker.Id);
            if (markerState is null)
            {
                continue;
            }

            marker.ChangeMarkerOffsetAndLength(markerState.Offset, markerState.Length);
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

    public void AddMarker(Marker marker)
    {
        InternalMarkers.Add(marker);
        _markersVersion++;
        OnMarkersChanged();
    }

    public void RemoveMarker(Marker marker)
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

    public async Task<bool> TryCopyAsync(long offset, long length, ActiveColumn? column,
        CancellationToken cancellationToken = default)
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
                var value = convertible.ToString(readBuffer.AsSpan(0, (int)length),
                    new FormatInfo(offset, Configuration));
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

    public async Task<bool> TryPasteAsync(long offset, ActiveColumn? column,
        CancellationToken cancellationToken = default)
    {
        return await TryPasteAsync(offset, null, column, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, long? length, CancellationToken cancellationToken = default)
    {
        return await TryPasteAsync(offset, length, null, cancellationToken);
    }

    public async Task<bool> TryPasteAsync(long offset, long? length, ActiveColumn? column,
        CancellationToken cancellationToken = default)
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
            ActiveColumn.Hex => Configuration.DataCharacterSet,
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
            ActiveColumn.Hex => Configuration.DataCharacterSet,
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

    #region Modifications

    public bool CanModify => !Buffer.IsReadOnly && !Buffer.Locked;

    public async Task<bool> TryTypeAtCaretAsync(char @char)
    {
        var endOfDocument = Caret.Offset >= Length;
        var isNewByte = Selection.HasValue || endOfDocument || Configuration.WriteMode is WriteMode.Insert && Caret.Nibble is 0;

        var characterSet = GetCharacterSetForColumn(Caret.Column);
        var oldByte = (byte)0;

        if (!isNewByte)
        {
            var readByte = await ReadCaretByte();
            if (readByte is null)
            {
                return false;
            }

            oldByte = readByte.Value;
        }

        // Write to byte and validate if it is possible to write this character
        if (!characterSet.TryWrite(oldByte, @char, Caret.Nibble, out var newByte))
        {
            return false;
        }

        var oldCaret = Caret with { };
        var redoOffset = oldCaret.Offset;

        if (Selection is { } selection)
        {
            await Buffer.ReplaceAsync(selection.Start, selection.Length, newByte);
            redoOffset = selection.Start;
        }
        else if (endOfDocument || Configuration.WriteMode is WriteMode.Insert && Caret.Nibble is 0)
        {
            await Buffer.InsertAsync(Caret.Offset, newByte);
        }
        else
        {
            await Buffer.WriteAsync(Caret.Offset, newByte);
        }

        var advanceOffset = oldCaret.Nibble >= characterSet.Width - 1;
        var redoCaret = Caret = oldCaret with
        {
            Offset = advanceOffset ? redoOffset + 1 : redoOffset,
            Nibble = advanceOffset ? 0 : oldCaret.Nibble + 1
        };

        // Typing is the only modification that can advance a nibble. 
        // Therefore, we need to replace the caret in the change tracker,
        // since the change tracker does not understand the concept of typing (and it should not).
        if (Buffer.ChangeTracking is not ChangeTracking.None)
        {
            var state = _undoStates.Pop();
            _undoStates.Push(state.Redo is null ? state : (state.Undo, state.Redo with
            {
                Caret = redoCaret
            }));
        }

        return true;
    }

    private async ValueTask<byte?> ReadCaretByte()
    {
        var readBuffer = ArrayPool<byte>.Shared.Rent(1);

        try
        {
            var readLength = await Buffer.ReadAsync(readBuffer.AsMemory(0, 1), Caret.Offset);
            if (readLength <= 0)
            {
                return null;
            }

            return readBuffer[0];
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(readBuffer);
        }
    }

    #endregion

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
        if (changesArray.Any(p => p is nameof(Configuration.ColumnsVisible) or nameof(Configuration.DataCharacterSet)
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

        ConfigurationChanged?.Invoke(this,
            new ConfigurationChangedEventArgs(oldConfiguration, newConfiguration, changesArray));
    }

    protected virtual void OnModified(IReadOnlyList<BufferModification> modifications, ModificationSource source)
    {
        Modified?.Invoke(this, new ModifiedEventArgs(modifications, source));
    }

    #endregion

    private bool IsVisibleColumn(ActiveColumn column, out ActiveColumn visibleColumn)
    {
        if (Configuration.ColumnsVisible is VisibleColumns.DataText)
        {
            visibleColumn = column;
            return true;
        }

        visibleColumn = column switch
        {
            ActiveColumn.Hex when Configuration.ColumnsVisible is VisibleColumns.Text => ActiveColumn.Text,
            ActiveColumn.Text when Configuration.ColumnsVisible is VisibleColumns.Data => ActiveColumn.Hex,
            _ => column
        };
        return false;
    }

    #region Modifications
    private delegate (Selection? UndoSelection, Selection? RedoSelection) AdjustSelectionDelegate();
    private delegate (Caret UndoSelection, Caret RedoSelection) AdjustCaretDelegate();
    private delegate (long Offset, long Length) AdjustRangeDelegate(long rangeOffset, long rangeLength,
        long adjustOffset, long adjustLength);

    private void ApplyModification(BufferModification modification)
    {
        var result = modification switch
        {
            DeleteModification(var offset, var length) => ApplyModification(
                offset, length,
                () =>
                {
                    var undoCaret = Caret with { };
                    var redoCaret = Caret with { };
                    if (offset < Caret.Offset)
                    {
                        redoCaret = Caret = Caret with
                        {
                            Offset = Caret.Offset - length,
                            Nibble = 0
                        };
                    }

                    return (undoCaret, redoCaret);
                },
                () =>
                {
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

                    return (undoSelection, redoSelection);
                },
                DeleteFromRange),
            InsertModification(var offset, var bytes) => ApplyModification(
                offset, bytes.Length,
                () =>
                {
                    var undoCaret = Caret with { };
                    var redoCaret = Caret with { };

                    if (Caret.Offset <= offset)
                    {
                        redoCaret = Caret = Caret with { Offset = offset + bytes.Length, Nibble = 0 };
                    }

                    return (undoCaret, redoCaret);
                },
                () =>
                {
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

                    return (undoSelection, redoSelection);
                },
                InsertIntoRange),
            WriteModification(var offset, var bytes) => ApplyModification(
                offset, bytes.Length,
                () => (Caret with { }, Caret with { }),
                () =>
                {
                    var selection = Selection.HasValue ? Selection with { } : null;
                    return (selection, selection);
                }),
            _ => throw new InvalidOperationException(
                $"Modification with type '{modification.GetType().Name}' is not yet supported.")
        };

        if (result is null)
        {
            return;
        }

        _undoStates.Push(result.Value);
        _redoStates.Clear();
    }

    private (DocumentState Undo, DocumentState? Redo)? ApplyModification(
        long offset,
        long length,
        AdjustCaretDelegate adjustCaret,
        AdjustSelectionDelegate adjustSelection,
        AdjustRangeDelegate? adjustRange = null)
    {
        var trackMarkers = adjustRange is not null && Buffer.ChangeTracking is not ChangeTracking.None;
        var undoMarkerStates =
            trackMarkers ? new DictionarySlim<Guid, MarkerState>() : EmptyMarkerStateDictionary;
        var redoMarkerStates =
            trackMarkers ? new DictionarySlim<Guid, MarkerState>() : EmptyMarkerStateDictionary;

        if (adjustRange is not null)
        {
            var count = InternalMarkers.Count;
            var span = CollectionsMarshal.AsSpan(InternalMarkers);
            for (var i = 0; i < count; i++)
            {
                var marker = span[i];
                var markerOffset = marker.Offset;
                var markerLength = marker.Length;

                var (newOffset, newLength) = adjustRange(markerOffset, markerLength, offset, length);
                if (newOffset != markerOffset || newLength != markerLength)
                {
                    if (Buffer.ChangeTracking is not ChangeTracking.None)
                    {
                        ref var undoRef = ref undoMarkerStates.GetOrAddValueRef(marker.Id);
                        undoRef = new MarkerState(markerOffset, markerLength);
                        ref var redoRef = ref redoMarkerStates.GetOrAddValueRef(marker.Id);
                        redoRef = new MarkerState(newOffset, newLength);
                    }

                    marker.ChangeMarkerOffsetAndLength(newOffset, newLength);
                }
            }
        }

        var (undoSelection, redoSelection) = adjustSelection();
        var (undoCaret, redoCaret) = adjustCaret();

        if (Buffer.ChangeTracking is ChangeTracking.None)
        {
            return null;
        }

        var undoState = new DocumentState(undoMarkerStates, undoSelection, undoCaret);
        var redoState = Buffer.ChangeTracking is ChangeTracking.Undo ? null : new DocumentState(redoMarkerStates, redoSelection, redoCaret);
        return (undoState, redoState);
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

        if (deleteOffset < offset && deleteEnd <= offset)
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
    #endregion

    #region State
    internal CapturedState CapturedState => _capturedState ??= CaptureState();

    internal CapturedState CaptureState()
    {
        if (InternalMarkers.Count > _capturedMarkers.Length)
        {
            _capturedMarkers = new Marker[InternalMarkers.Count * 2];
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
    #endregion
}

internal readonly record struct CapturedState(long Offset, long Length, Selection? Selection, Caret Caret, Memory<Marker> Markers, DocumentConfiguration Configuration);