using Xunit;

namespace HexControl.Buffers.Tests;

public partial class ByteBufferTests
{
    [Fact]
    public void Insert_InVirtualChunk()
    {
        _buffer.ValidateInsert(100, SampleBytes, expects =>
            expects
                .Length(556)
                .Immutable(100, 0)
                .Memory(SampleBytes)
                .Immutable(446, 100));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Insert_InMemoryChunk()
    {
        _buffer.ValidateInsert(100, SampleBytes); // ignore

        _buffer.ValidateInsert(105, SampleBytes, expects =>
            expects
                .Length(566)
                .Immutable(100, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 5, 6, 7, 8, 9})
                .Immutable(446, 100));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Insert_AtStartOfBuffer()
    {
        _buffer.ValidateInsert(0, SampleBytes, expects =>
            expects
                .Length(556)
                .Memory(SampleBytes)
                .Immutable(546, 0));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Insert_AtBeginningOfVirtualChunk_WhenPreviousIsMemoryChunk()
    {
        _buffer.ValidateInsert(100, SampleBytes); // ignore

        _buffer.ValidateInsert(110, SampleBytes, expects =>
            expects
                .Length(566)
                .Immutable(100, 0)
                .Memory(Combine(SampleBytes, SampleBytes))
                .Immutable(446, 100));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Insert_InBetween_TwoVirtualChunks()
    {
        var bytes = new byte[]
        {
            84, 104, 105, 115, 32, 112, 114, 111, 103, 114, 97, 109, 32, 99, 97, 110, 110, 111, 116, 32, 98, 101, 32,
            114, 117, 110, 32, 105, 110, 32, 68, 79, 83, 32, 109, 111, 100, 101
        };

        _buffer.Delete(115, 36);

        _buffer.ValidateInsert(115, bytes, expects =>
            expects
                .Length(548)
                .Immutable(115, 0)
                .Memory(bytes)
                .Immutable(395, 151));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Insert_AtEndOfBuffer()
    {
        _buffer.ValidateInsert(546, SampleBytes, expects =>
            expects
                .Length(556)
                .Immutable(546, 0)
                .Memory(SampleBytes));
        _buffer.ValidateUndoRedo();
    }
}