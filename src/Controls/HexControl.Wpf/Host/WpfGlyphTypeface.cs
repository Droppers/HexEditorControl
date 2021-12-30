using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Threading;
using System.Windows.Media;
using HexControl.SharedControl.Framework;
using HexControl.SharedControl.Framework.Drawing.Text;
using HexControl.SharedControl.Framework.Host.Typeface;

namespace HexControl.Wpf.Host;

internal class WpfGlyphTypeface : CachedGlyphTypeface<GlyphTypeface>
{
    private double? _advanceWidth;

    public WpfGlyphTypeface(string typefaceName)
    {
        var typeface = new Typeface(typefaceName);
        if (!typeface.TryGetGlyphTypeface(out var glyphTypeface))
        {
            throw new InvalidOperationException($"Could not get TypeFace: {typefaceName}");
        }

        Typeface = glyphTypeface;
    }

    public WpfGlyphTypeface(EmbeddedAsset asset)
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        var assembly = assemblies.SingleOrDefault(a => a.ManifestModule.Name.Equals(asset.Assembly + ".dll"));
        var resource = assembly?.GetManifestResourceStream($"{asset.Assembly}.{asset.File}");
        if (resource is null)
        {
            throw new ArgumentException($"Could not find asset {asset.File} in assembly {asset.Assembly}.",
                nameof(asset));
        }

        using var memoryPackage = new MemoryPackage();
        var typefaceSource = memoryPackage.CreatePart(resource);
        Typeface = new GlyphTypeface(typefaceSource);
        memoryPackage.DeletePart(typefaceSource);
    }

    public override GlyphTypeface Typeface { get; }

    public override bool TryGetGlyphIndexInternal(int codePoint, out ushort glyphIndex) =>
        Typeface.CharacterToGlyphMap.TryGetValue(codePoint, out glyphIndex);

    public override double GetHeight(double size) => Typeface.CapsHeight * size;

    public override double GetWidth(double size)
    {
        _advanceWidth ??= Typeface.AdvanceWidths['W'];
        return _advanceWidth.Value * size;
    }

    public override double GetGlyphOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException($"TextAlignment {alignment} is not supported");
        }

        return Math.Ceiling(GetHeight(size));
    }

    public override double GetTextOffsetY(TextAlignment alignment, double size)
    {
        if (alignment is not TextAlignment.Top)
        {
            throw new NotSupportedException($"TextAlignment {alignment} is not supported");
        }

        var offset = Typeface.Baseline * size - GetHeight(size);
        return Math.Ceiling(-offset); //GetHeight(size);
    }

    private class MemoryPackage : IDisposable
    {
        private static int _packageCounter;

        private readonly Package _package = Package.Open(new MemoryStream(), FileMode.Create);

        private readonly Uri _packageUri = new("payload://memorypackage" + Interlocked.Increment(ref _packageCounter),
            UriKind.Absolute);

        private int _partCounter;

        public MemoryPackage()
        {
            PackageStore.AddPackage(_packageUri, _package);
        }

        public void Dispose()
        {
            PackageStore.RemovePackage(_packageUri);
            _package.Close();
        }

        public Uri CreatePart(Stream source, string contentType = "application/octet-stream")
        {
            var partUri = new Uri("/stream" + ++_partCounter, UriKind.Relative);

            var part = _package.CreatePart(partUri, contentType);
            using var partStream = part.GetStream();
            CopyStream(source, partStream);

            return PackUriHelper.Create(_packageUri, partUri);
        }

        public void DeletePart(Uri packUri)
        {
            _package.DeletePart(PackUriHelper.GetPartUri(packUri)!);
        }

        private static void CopyStream(Stream source, Stream destination)
        {
            const int bufferSize = 4096;

            var buffer = new byte[bufferSize];
            int read;
            while ((read = source.Read(buffer, 0, buffer.Length)) != 0)
            {
                destination.Write(buffer, 0, read);
            }
        }
    }
}