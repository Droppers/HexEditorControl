using HexControl.SharedControl.Characters;
using JetBrains.Annotations;
using System.Diagnostics;

namespace HexControl.SharedControl.Documents;

[PublicAPI]
public record DocumentConfiguration
{
    public static readonly DocumentConfiguration Default = new();

    public bool OffsetsVisible { get; init; } = true;

    public VisibleColumns ColumnsVisible { get; init; } = VisibleColumns.DataText;

    public NumberBase OffsetBase { get; init; } = NumberBase.Hexadecimal;

    public int BytesPerRow { get; init; } = 16;

    public int GroupSize { get; init; } = 4;

    public CharacterSet DataCharacterSet { get; init; } = new HexCharacterSet();

    public CharacterSet TextCharacterSet { get; init; } = new TextCharacterSet(CharacterEncoding.Windows);

    public WriteMode WriteMode { get; init; } = WriteMode.Overwrite;

    [StackTraceHidden]
    public void Verify()
    {
        if ((DataCharacterSet.ByteWidth > 1 || TextCharacterSet.ByteWidth > 1) && GroupSize is not 1)
        {
            throw new InvalidConfigurationException(nameof(GroupSize), "Group size cannot be greater than '1' when using a grouped (multiple byte width) character set.");
        }

        var maxByteWidth = Math.Max(DataCharacterSet.ByteWidth, TextCharacterSet.ByteWidth);
        if(BytesPerRow % maxByteWidth is not 0)
        {
            throw new InvalidConfigurationException(nameof(BytesPerRow), $"Bytes per row ('{BytesPerRow}') cannot be divided by max byte width ('{maxByteWidth}').");
        }

        if(GroupSize > 1 && BytesPerRow % GroupSize is not 0)
        {
            throw new InvalidConfigurationException(nameof(BytesPerRow), $"Bytes per row ('{BytesPerRow}') cannot be divided by group size ('{GroupSize}').");
        }
    }
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

        if (DataCharacterSet != other.DataCharacterSet)
        {
            yield return nameof(DataCharacterSet);
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
