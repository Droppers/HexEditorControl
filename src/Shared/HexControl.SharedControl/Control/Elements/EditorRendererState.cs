using HexControl.Framework.Drawing;
using HexControl.Framework.Optimizations;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Documents;
using System.Drawing;
using Timer = System.Timers.Timer;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorRendererState
{
    private IDocumentMarker? _inactiveMarker;
    private IDocumentMarker? _activeMarker;

    private ISharedBrush?[] _markerForegroundLookup;

    private readonly Dictionary<IDocumentMarker, MarkerRange> _previousMarkers;
    private readonly ObjectCache<Color, ISharedBrush> _colorToBrushCache;

    private readonly SynchronizationContext? _syncContext;
    private readonly VisualElement _owner;
    private bool _caretTick;
    private long _previousCaretOffset;
    private Timer? _caretTimer;
    private bool _caretUpdated;

    public EditorRendererState(VisualElement owner)
    {
        _syncContext = SynchronizationContext.Current;
        _caretTimer = CreateCaretTimer();
        _owner = owner;
        _previousMarkers = new Dictionary<IDocumentMarker, MarkerRange>(50);
        _colorToBrushCache = new ObjectCache<Color, ISharedBrush>(color => new ColorBrush(color));
        _markerForegroundLookup = Array.Empty<ISharedBrush?>();
    }

    public IDocumentMarker? ActiveMarker
    {
        get => _activeMarker;
        set => _activeMarker = value;
    }

    public IDocumentMarker? InactiveMarker
    {
        get => _inactiveMarker;
        set => _inactiveMarker = value;
    }

    internal Dictionary<IDocumentMarker, MarkerRange> PreviousMarkers => _previousMarkers;
    internal ObjectCache<Color, ISharedBrush> ColorToBrushCache => _colorToBrushCache;

    public ISharedBrush?[] MarkerForegroundLookup
    {
        get => _markerForegroundLookup;
        set => _markerForegroundLookup = value;
    }

    public bool CaretUpdated
    {
        get => _caretUpdated;
        set => _caretUpdated = value;
    }

    public long PreviousCaretOffset
    {
        get => _previousCaretOffset;
        set => _previousCaretOffset = value;
    }

    public bool CaretTick
    {
        get => _caretTick;
        set => _caretTick = value;
    }

    private Timer CreateCaretTimer()
    {
        var timer = new Timer
        {
            Interval = 500,
            Enabled = true
        };
        timer.Elapsed += (_, _) =>
        {
            CaretTick = !CaretTick;
            CaretUpdated = true;

            if (_syncContext is not null)
            {
                _syncContext.Post(_ => _owner.Invalidate(), null);
            }
            else
            {
                _owner.Invalidate();
            }
        };

        return timer;
    }

    public void ResetCaretTick()
    {
        if (_caretTimer is null)
        {
            return;
        }

        CaretTick = true;
        _caretTimer.Stop();
        _caretTimer.Start();
    }
}