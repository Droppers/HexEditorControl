#if SKIA_RENDER
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using HexControl.Renderer.Skia;
using SkiaSharp;

namespace HexControl.Wpf;

internal class SKElement : FrameworkElement
{
    [Category("Appearance")] public event EventHandler<SKPaintSurfaceEventArgs>? PaintSurface;

    private const double BitmapDpi = 96.0;

    private readonly bool _designMode;

    private WriteableBitmap? _bitmap;
    private bool _ignorePixelScaling;

    public SKElement()
    {
        _designMode = DesignerProperties.GetIsInDesignMode(this);
    }

    public SKSize CanvasSize { get; private set; }

    public bool IgnorePixelScaling
    {
        get => _ignorePixelScaling;
        set
        {
            _ignorePixelScaling = value;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        if (_designMode)
        {
            return;
        }

        if (Visibility != Visibility.Visible || PresentationSource.FromVisual(this) == null)
        {
            return;
        }

        var size = CreateSize(out var unscaledSize, out var scaleX, out var scaleY);
        var userVisibleSize = IgnorePixelScaling ? unscaledSize : size;

        CanvasSize = userVisibleSize;

        if (size.Width <= 0 || size.Height <= 0)
        {
            return;
        }

        var info = new SKImageInfo(size.Width, size.Height, SKImageInfo.PlatformColorType, SKAlphaType.Premul);

        // reset the bitmap if the size has changed
        if (_bitmap == null || info.Width != _bitmap.PixelWidth || info.Height != _bitmap.PixelHeight)
        {
            _bitmap = new WriteableBitmap(info.Width, size.Height, BitmapDpi * scaleX, BitmapDpi * scaleY,
                PixelFormats.Pbgra32, null);
        }

        // draw on the bitmap
        _bitmap.Lock();
        using (var surface = SKSurface.Create(info, _bitmap.BackBuffer, _bitmap.BackBufferStride))
        {
            if (IgnorePixelScaling)
            {
                var canvas = surface.Canvas;
                canvas.Scale(scaleX, scaleY);
                canvas.Save();
            }

            OnPaintSurface(new SKPaintSurfaceEventArgs(surface, info.WithSize(userVisibleSize), info));
        }

        // draw the bitmap to the screen
        _bitmap.AddDirtyRect(new Int32Rect(0, 0, info.Width, size.Height));
        _bitmap.Unlock();
        drawingContext.DrawImage(_bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
    }

    protected virtual void OnPaintSurface(SKPaintSurfaceEventArgs e)
    {
        // invoke the event
        PaintSurface?.Invoke(this, e);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        InvalidateVisual();
    }

    private SKSizeI CreateSize(out SKSizeI unscaledSize, out float scaleX, out float scaleY)
    {
        unscaledSize = SKSizeI.Empty;
        scaleX = 1.0f;
        scaleY = 1.0f;

        var w = ActualWidth;
        var h = ActualHeight;

        if (!IsPositive(w) || !IsPositive(h))
        {
            return SKSizeI.Empty;
        }

        unscaledSize = new SKSizeI((int)w, (int)h);

        var m = PresentationSource.FromVisual(this)!.CompositionTarget.TransformToDevice;
        scaleX = (float)m.M11;
        scaleY = (float)m.M22;
        return new SKSizeI((int)(w * scaleX), (int)(h * scaleY));

        bool IsPositive(double value) => !double.IsNaN(value) && !double.IsInfinity(value) && value > 0;
    }
}

#endif