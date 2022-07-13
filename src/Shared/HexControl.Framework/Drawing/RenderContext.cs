using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Host;
using HexControl.Framework.Optimizations;

namespace HexControl.Framework.Drawing;

internal abstract class RenderContext<TNativeBrush, TNativePen> : IRenderContext
    where TNativeBrush : class
    where TNativePen : class
{
    private readonly ObjectCache<ISharedBrush, TNativeBrush?> _brushes;
    private readonly RenderFactory<TNativeBrush, TNativePen> _factory;
    private readonly LinkedList<IDisposable> _garbage;
    private readonly ObjectCache<ISharedPen, TNativePen?> _pens;

    private IRenderStateProvider? _stateProvider;

    protected RenderContext(RenderFactory<TNativeBrush, TNativePen> factory)
    {
        _factory = factory;
        _brushes = new ObjectCache<ISharedBrush, TNativeBrush?>(brush =>
            _factory.CreateBrush(brush));
        _pens = new ObjectCache<ISharedPen, TNativePen?>(pen => _factory.CreatePen(pen));

        _garbage = new LinkedList<IDisposable>();
    }

    public RenderFactory Factory => _factory;

    public virtual bool Synchronous => true;

    public virtual bool RequiresClear => false;
    public virtual bool PreferTextLayout => false;
    public virtual bool DirtyRect => false;

    public void CollectGarbage()
    {
        var currentNode = _garbage.First;
        while (currentNode != null)
        {
            var nextNode = currentNode.Next;
            currentNode.Value.Dispose();
            _garbage.Remove(currentNode);
            currentNode = nextNode;
        }
    }

    public bool CanRender { get; set; }

    public void Clear(ISharedBrush? brush)
    {
        if (CanRender)
        {
            Clear(_brushes[brush]);
        }
    }

    public void DrawRectangle(ISharedBrush? brush, ISharedPen? pen, SharedRectangle rectangle)
    {
        if (CanRender)
        {
            DrawRectangle(_brushes[brush], _pens[pen], rectangle);
        }
    }

    public void DrawPolygon(ISharedBrush? brush, ISharedPen? pen, ReadOnlySpan<SharedPoint> points)
    {
        if (CanRender)
        {
            DrawPolygon(_brushes[brush], _pens[pen], points);
        }
    }

    public void DrawLine(ISharedPen? pen, SharedPoint startPoint, SharedPoint endPoint)
    {
        if (CanRender)
        {
            DrawLine(_pens[pen], startPoint, endPoint);
        }
    }

    public void DrawGlyphRun(ISharedBrush? brush, SharedGlyphRun glyphRun)
    {
        if (CanRender)
        {
            DrawGlyphRun(_brushes[brush], glyphRun);
        }
    }

    public void DrawTextLayout(ISharedBrush? brush, SharedTextLayout layout)
    {
        if (CanRender)
        {
            DrawTextLayout(_brushes[brush], layout);
        }
    }

    public abstract void PushTranslate(double offsetX, double offsetY);

    public abstract void PushClip(SharedRectangle rectangle);

    public abstract void Pop();


    public virtual void Begin() { }

    public virtual void End(SharedRectangle? dirtyRect) { }

    public virtual void Dispose()
    {
        if (_stateProvider is not null)
        {
            _stateProvider.RenderStateChanged -= OnRenderStateChanged;
            CanRender = false;
        }

        _brushes.Dispose();
        _pens.Dispose();
        CollectGarbage();
    }

    public void AttachStateProvider(IRenderStateProvider provider)
    {
        _stateProvider = provider;
        CanRender = provider.CanRender;
        provider.RenderStateChanged += OnRenderStateChanged;
    }

    private void OnRenderStateChanged(object? sender, RenderStateChangedEventArgs e)
    {
        CanRender = e.CanRender;
    }

    protected TNativeBrush? GetBrush(ISharedBrush? brush) => _brushes[brush];

    protected TNativePen? GetPen(ISharedPen? pen) => _pens[pen];

    protected void AddGarbage(IDisposable disposable)
    {
        _garbage.AddLast(disposable);
    }

    protected abstract void Clear(TNativeBrush? brush);

    protected abstract void DrawRectangle(TNativeBrush? brush, TNativePen? pen, SharedRectangle rectangle);
    protected abstract void DrawLine(TNativePen? pen, SharedPoint startPoint, SharedPoint endPoint);
    protected abstract void DrawPolygon(TNativeBrush? brush, TNativePen? pen, ReadOnlySpan<SharedPoint> points);
    protected abstract void DrawGlyphRun(TNativeBrush? brush, SharedGlyphRun sharedGlyphRun);
    protected abstract void DrawTextLayout(TNativeBrush? brush, SharedTextLayout layout);
}