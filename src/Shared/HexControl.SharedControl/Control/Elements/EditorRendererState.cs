using HexControl.Framework.Caching;
using HexControl.Framework.Drawing;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Documents;
using System.Drawing;
using Timer = System.Timers.Timer;

namespace HexControl.SharedControl.Control.Elements;

internal class EditorRendererState
{
    private readonly SynchronizationContext? _syncContext;
    private readonly VisualElement _owner;
    private readonly Timer? _caretTimer;

    public EditorRendererState(VisualElement owner)
    {
        _syncContext = SynchronizationContext.Current;
        _caretTimer = CreateCaretTimer();
        _owner = owner;
        PreviousMarkers = new Dictionary<Marker, MarkerRange>(50);
        ColorToBrushCache = new ObjectCacheSlim<Color, ISharedBrush>(color => new ColorBrush(color));
        MarkerForegroundLookup = Array.Empty<ISharedBrush?>();
    }

    public Marker? ActiveMarker { get; set; }

    public Marker? InactiveMarker { get; set; }

    internal Dictionary<Marker, MarkerRange> PreviousMarkers { get; }

    internal ObjectCacheSlim<Color, ISharedBrush> ColorToBrushCache { get; }

    public ISharedBrush?[] MarkerForegroundLookup { get; set; }

    public bool CaretUpdated { get; set; }

    public long PreviousCaretOffset { get; set; }

    public bool CaretTick { get; set; }

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

            try
            {
                if (_syncContext is not null)
                {
                    _syncContext.Post(_ => _owner.Invalidate(), null);
                }
                else
                {
                    _owner.Invalidate();
                }
            }
            catch
            {
                // ignore
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

        _caretTimer.Stop();
        _caretTimer.Start();
        CaretTick = true;
    }
}