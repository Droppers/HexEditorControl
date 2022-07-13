using HexControl.Framework.Drawing.Text;
using HexControl.Framework.Drawing;
using HexControl.Framework.Visual;
using HexControl.SharedControl.Documents;
using HexControl.SharedControl.Control.Helpers;

namespace HexControl.SharedControl.Control.Elements;

internal readonly ref struct OffsetRenderer
{
    private readonly SharedHexControl _control;
    private readonly VisualElement _owner;
    private readonly CapturedState _documentState;
    private readonly IRenderContext _context;
    private readonly ITextBuilder _textBuilder;

    public OffsetRenderer(
        SharedHexControl control,
        VisualElement owner,
        CapturedState documentState,
        IRenderContext context,
        ITextBuilder textBuilder)
    {
        _control = control;
        _owner = owner;
        _documentState = documentState;
        _context = context;
        _textBuilder = textBuilder;
    }

    public void Render()
    {
        if (_textBuilder is null)
        {
            return;
        }

        _textBuilder.Clear();
        WriteOffsetHeader();
        WriteContentOffsets();
        _textBuilder.Draw(_context);
    }

    private void WriteOffsetHeader()
    {
        _textBuilder.Next(new SharedPoint(0, 0));
        _textBuilder.Add(_control.HeaderForeground, _control.OffsetHeader);
    }

    private void WriteContentOffsets()
    {
        var configuration = _documentState.Configuration;

        Span<char> characterBuffer = stackalloc char[30];
        var maxLength = BaseConverter.Convert(_documentState.Length, configuration.OffsetBase, true, characterBuffer);
        var zeroPadCount = Math.Max(_control.OffsetHeader.Length, maxLength);


        var elementHeight = _owner.Height;
        var rowCount = elementHeight / _control.RowHeight;
        var glyphMiddleOffset = (int)Math.Round(_control.RowHeight / 2d - _control.CharacterHeight / 2d);

        for (var row = 0; row < rowCount; row++)
        {
            var y = row * _control.RowHeight + glyphMiddleOffset + _control.HeaderHeight;

            var offset = _documentState.Offset + row * configuration.BytesPerRow;
            _textBuilder.Next(new SharedPoint(0, y));

            var length = BaseConverter.Convert(offset, configuration.OffsetBase, true, characterBuffer);
            var padZeros = zeroPadCount - length;
            for (var i = 0; i < length + padZeros; i++)
            {
                var charIndex = length - (i - padZeros) - 1;
                var @char = charIndex >= length ? '0' : characterBuffer[charIndex];
                _textBuilder.Add(_control.OffsetForeground, @char);
            }

            if (y > elementHeight || offset + configuration.BytesPerRow >= _documentState.Length)
            {
                break;
            }
        }
    }
}