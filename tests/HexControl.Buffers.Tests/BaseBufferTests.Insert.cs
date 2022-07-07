using Xunit;

namespace HexControl.Buffers.Tests;

public partial class BaseBufferTests
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