using HexControl.SharedControl.Characters;
using JetBrains.Annotations;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public record DocumentConfiguration
{
    public static readonly DocumentConfiguration Default = new();

    public bool OffsetsVisible { get; init; } = true;

    public VisibleColumns ColumnsVisible { get; init; } = VisibleColumns.HexText;

    public NumberBase OffsetBase { get; init; } = NumberBase.Hexadecimal;

    public int BytesPerRow { get; init; } = 16;

    public int GroupSize { get; init; } = 4;

    public CharacterSet HexCharacterSet { get; init; } = new HexCharacterSet();

    public CharacterSet TextCharacterSet { get; init; } = new TextCharacterSet(CharacterEncoding.Windows);

    public WriteMode WriteMode { get; init; } = WriteMode.Overwrite;

    public IEnumerable<string> DetectChanges(DocumentConfiguration other)
    {
        if (OffsetsVisible != other.OffsetsVisible)
        {
            yield return nameof(OffsetsVisible);
        }
        
        if (ColumnsVisible != other.ColumnsVisible)
        {
            yield return nameof(ColumnsVisible);
        }

        if (OffsetBase != other.OffsetBase)
        {
            yield return nameof(OffsetBase);
        }

        if (BytesPerRow != other.BytesPerRow)
        {
            yield return nameof(BytesPerRow);
        }

        if (GroupSize != other.GroupSize)
        {
            yield return nameof(GroupSize);
        }

        if (HexCharacterSet != other.HexCharacterSet)
        {
            yield return nameof(HexCharacterSet);
        }

        if (TextCharacterSet != other.TextCharacterSet)
        {
            yield return nameof(TextCharacterSet);
        }

        if (WriteMode != other.WriteMode)
        {
            yield return nameof(WriteMode);
        }
    }
}
