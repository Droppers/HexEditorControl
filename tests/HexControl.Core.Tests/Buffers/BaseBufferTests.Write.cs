using Xunit;

namespace HexControl.Core.Tests.Buffers;

public partial class BaseBufferTests
{
    [Fact]
    public void Write_InVirtualChunk()
    {
        _buffer.ValidateWrite(100, SampleBytes, expects =>
            expects
                .Length(546)
                .File(100, 0)
                .Memory(SampleBytes)
                .File(436, 110));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_InMemoryChunk()
    {
        _buffer.ValidateWrite(100, Combine(SampleBytes, SampleBytes)); // ignore

        _buffer.ValidateWrite(102, new byte[] {4, 3, 2, 1, 0}, expects =>
            expects
                .Length(546)
                .File(100, 0)
                .Memory(new byte[] {0, 1, 4, 3, 2, 1, 0, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
                .File(426, 120));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtEndOfMemoryChunk_RemoveStartOfVirtualChunk()
    {
        _buffer.ValidateWrite(100, SampleBytes); // ignore

        _buffer.ValidateWrite(105, SampleBytes, expects =>
            expects
                .Length(546)
                .File(100, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
                .File(431, 115));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_OverMultipleMemoryChunks()
    {
        _buffer.ValidateWrite(100, SampleBytes); // ignore
        _buffer.ValidateWrite(120, SampleBytes); // ignore
        _buffer.ValidateWrite(140, SampleBytes); // ignore

        _buffer.ValidateWrite(80, new byte[100], expects =>
            expects
                .Length(546)
                .File(80, 0)
                .Memory(new byte[100])
                .File(366, 180));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_InAndOverMultipleMemoryChunks()
    {
        _buffer.ValidateWrite(100, SampleBytes); // ignore
        _buffer.ValidateWrite(120, SampleBytes); // ignore
        _buffer.ValidateWrite(140, SampleBytes); // ignore

        _buffer.ValidateWrite(105, new byte[100], expects =>
            expects
                .Length(546)
                .File(100, 0)
                .Memory(PadRight(new byte[] {0, 1, 2, 3, 4}, 105))
                .File(341, 205));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_StartOfVirtualChunk_WhenPreviousIsMemoryChunk()
    {
        var bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        _buffer.ValidateWrite(100, bytes);

        _buffer.ValidateWrite(110, bytes, expects =>
            expects
                .Length(546)
                .File(100, 0)
                .Memory(Combine(bytes, bytes))
                .File(426, 120));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_EndOfVirtualChunk_WhenNextIsMemoryChunk()
    {
        var bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        _buffer.ValidateWrite(100, bytes);

        _buffer.ValidateWrite(95, bytes, expects =>
            expects
                .Length(546)
                .File(95, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 5, 6, 7, 8, 9})
                .File(436, 110));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtStartOfBuffer()
    {
        var bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        _buffer.ValidateWrite(0, bytes, expects =>
            expects
                .Length(546)
                .Memory(bytes)
                .File(536, 10));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtEndOfBuffer()
    {
        var bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        _buffer.ValidateWrite(546, bytes, expects =>
            expects
                .Length(556)
                .File(546, 0)
                .Memory(bytes));
        _buffer.ValidateUndoRedo();
    }
}