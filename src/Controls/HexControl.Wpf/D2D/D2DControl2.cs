using SharpDX.Direct2D1;
using System.Windows;

namespace HexControl.Wpf.D2D
{
    internal delegate void RenderEvent2(Direct2DImageSurface surface, Factory factory, RenderTarget renderTarget, bool newSurface);

    internal class D2DControl2 : System.Windows.Controls.Image
    {
        private readonly Direct2DImageSurface _surface;
        private RenderTarget? _renderTarget;
        private bool _newSurface;

        public event RenderEvent2? Render;

        private float _dpi = 1;

        public float Dpi
        {
            get => _dpi;
            set
            {
                _dpi = value;
                RecreateTarget();
            }
        }

        public D2DControl2(FrameworkElement parent)
        {
            _surface = new Direct2DImageSurface(parent, this);
            parent.SizeChanged += D2DControl2_SizeChanged;
        }

        private void D2DControl2_SizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            RecreateTarget();
        }

        private void RecreateTarget()
        {
            _surface.DestroyRenderTarget();
            _renderTarget = _surface.GetOrCreateRenderTarget();
            _newSurface = true;
        }

        public void Invalidate()
        {
            _renderTarget ??= _surface.GetOrCreateRenderTarget() ?? throw new InvalidOperationException("Could not create d3d render target.");

            Render?.Invoke(_surface, _surface.D2DFactory, _renderTarget, _newSurface);

            _newSurface = false;
        }
    }
}
