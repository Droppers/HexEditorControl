using HexControl.SharedControl.Documents;

namespace HexControl.SharedControl.Characters;

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