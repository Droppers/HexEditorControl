using HexControl.SharedControl.Control.Helpers;
using HexControl.Framework.Drawing;
using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Documents;
using DocumentConfiguration = HexControl.SharedControl.Documents.DocumentConfiguration;

namespace HexControl.SharedControl.Control.Elements;

internal class OffsetElement : VisualElement
{
    private readonly char[] _characterBuffer = new char[20];
    private readonly SharedHexControl _control;
    private long _length;
    private int _zeroPadCount;

    public OffsetElement(SharedHexControl control)
    {
        _control = control;
    }

    public override double Width => (_zeroPadCount + 2) * _control.CharacterWidth;

    public long Length
    {
        get => _length;
        set
        {
            _length = value;
            _zeroPadCount = Math.Max(_control.OffsetHeader.Length,
                BaseConverter.Convert(value, Configuration.OffsetBase, true, _characterBuffer));
        }
    }

    public long Offset { get; set; }
    public DocumentConfiguration Configuration { get; set; } = DocumentConfiguration.Default;

    public Document? Document { get; set; }
    public ITextBuilder? TextBuilder { get; set; }

    protected override void Render(IRenderContext context)
    {
        if (TextBuilder is null || Document is null)
        {
            return;
        }

        new OffsetRenderer(_control, this, Document.CapturedState, context, TextBuilder).Render();
    }
}