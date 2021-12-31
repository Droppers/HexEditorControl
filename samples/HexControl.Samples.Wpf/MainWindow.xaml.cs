using System;
using System.Drawing;
using System.Windows;
using HexControl.Core;
using HexControl.SharedControl.Control;
using HexControl.SharedControl.Framework.Drawing;

namespace HexControl.Samples.Wpf;

// TODO: Just a test with terrible code, create a proper example for future documentation.
public class TestApi : HexRenderApi
{
    private readonly ISharedPen _borderPen =
        new SharedPen(new ColorBrush(Color.FromArgb(100, 255, 255, 255)), 1, PenStyle.Dashed);

    private readonly ISharedBrush _brush = new ColorBrush(Color.FromArgb(40, 44, 52));
    private readonly ISharedBrush _highlightBrush = new ColorBrush(Color.FromArgb(60, 70, 180, 255));

    public override void BeforeRender(IRenderContextApi context, Details details)
    {
        //context.DrawRectangle(new ColorBrush(Color.FromArgb(100, 0,0,0)), null, 0, 0, details.Offset.Value.Rectangle.Width + details.Offset.Value.Rectangle.X, details.Size.Height);
        //context.DrawRectangle(new ColorBrush(Color.FromArgb(100, 0, 0, 0)), null, details.Offset.Value.Rectangle.Width + details.Offset.Value.Rectangle.X, 0, details.Size.Width, details.Control.HeaderHeight + 10);

        var leftDimen = details.Editor.LeftRectangle;
        var leftRect = new SharedRectangle(details.Editor.Rectangle.X + Math.Max(0, leftDimen.X) - 5,
            details.Editor.Rectangle.Y + leftDimen.Y - 2, details.Editor.LeftVisibleWidth + 10, leftDimen.Height + 4);
        context.DrawRectangle(_brush, null, leftRect);

        var rightDimen = details.Editor.RightRectangle.Value;
        var rightRect = new SharedRectangle(details.Editor.Rectangle.X + Math.Max(0, rightDimen.X) - 5,
            details.Editor.Rectangle.Y + rightDimen.Y - 2, details.Editor.RightVisibleWidth + 10,
            rightDimen.Height + 4);
        context.DrawRectangle(_brush, null, rightRect);

        var totalWidth = details.Editor.Rectangle.X + rightDimen.X + details.Editor.RightVisibleWidth -
                         details.Offset.Value.Rectangle.X;
        var document = details.Control.Document;
        if (document is not null && document.Selection is null && document.Cursor.Offset >= document.Offset)
        {
            var cursor = document.Cursor;
            var row = (cursor.Offset - document.Offset) / document.Configuration.BytesPerRow;

            var y = row * details.Control.RowHeight + details.Editor.Rectangle.Y + leftDimen.Y;
            context.DrawRectangle(_highlightBrush, null,
                new SharedRectangle(details.Offset.Value.Rectangle.X - 2, y, totalWidth + 4,
                    details.Control.RowHeight));
        }
    }

    public override void AfterRender(IRenderContextApi context, Details details)
    {
        var document = details.Control.Document;

        var groupCount = document.Configuration.BytesPerRow / document.Configuration.GroupSize;
        var startLeft = details.Editor.Rectangle.X;
        for (var i = 0; i < groupCount; i++)
        {
            if (i != 0)
            {
                var left = startLeft + .5 - details.Control.CharacterWidth / 2;
                var top = details.Editor.Rectangle.Y + details.Editor.LeftRectangle.Y;
                context.DrawLine(_borderPen, new SharedPoint(left, top),
                    new SharedPoint(left, top + details.Editor.LeftRectangle.Height));
            }

            startLeft += (document.Configuration.GroupSize * document.Configuration.LeftCharacterSet.Width + 1) *
                         details.Control.CharacterWidth;
        }
    }
}

public partial class MainWindow : Window
{
    private readonly Document _doc;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
        
        _doc = Document.FromFile(@"..\..\..\..\..\files\sample-binary");
        _doc.Buffer.Write(38, new byte[100]);

        Document = _doc;
    }

    public Document Document { get; set; }
    public HexRenderApi Api { get; set; } = new TestApi();

    private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Document.Buffer.Undo();
        }
        catch { }
    }

    private void Clear_OnClick(object sender, RoutedEventArgs e)
    {
        EditorControl.Document = null!;
    }

    private void Restore_OnClick(object sender, RoutedEventArgs e)
    {
        EditorControl.Document = _doc;
    }

    private void Insert_OnClick(object sender, RoutedEventArgs e)
    {
        _doc.Buffer.Insert(_doc.Cursor.Offset, new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10});
    }
}