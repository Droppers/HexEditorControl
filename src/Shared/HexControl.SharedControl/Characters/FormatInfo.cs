using HexControl.SharedControl.Documents;
using JetBrains.Annotations;

namespace HexControl.SharedControl.Characters;

[PublicAPI]
public ref struct FormatInfo
{
    public FormatInfo(long offset, DocumentConfiguration configuration)
    {
        Offset = offset;
        Configuration = configuration;
    }

    public long Offset { get; }

    public DocumentConfiguration Configuration { get; }
}