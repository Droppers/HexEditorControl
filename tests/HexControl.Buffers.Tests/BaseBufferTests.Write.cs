using Xunit;

namespace HexControl.Buffers.Tests;

public partial class ByteBufferTests
{
    [Fact]
    public void Write_InVirtualChunk()
    {
        _buffer.ValidateWrite(100, SampleBytes, expects =>
            expects
                .Length(546)
                .Immutable(100, 0)
                .Memory(SampleBytes)
                .Immutable(436, 110));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_InMemoryChunk()
    {
        _buffer.ValidateWrite(100, Combine(SampleBytes, SampleBytes)); // ignore

        _buffer.ValidateWrite(102, new byte[] {4, 3, 2, 1, 0}, expects =>
            expects
                .Length(546)
                .Immutable(100, 0)
                .Memory(new byte[] {0, 1, 4, 3, 2, 1, 0, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
                .Immutable(426, 120));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtEndOfMemoryChunk_RemoveStartOfVirtualChunk()
    {
        _buffer.ValidateWrite(100, SampleBytes); // ignore

        _buffer.ValidateWrite(105, SampleBytes, expects =>
            expects
                .Length(546)
                .Immutable(100, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9})
                .Immutable(431, 115));

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
                .Immutable(80, 0)
                .Memory(new byte[100])
                .Immutable(366, 180));

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
                .Immutable(100, 0)
                .Memory(PadRight(new byte[] {0, 1, 2, 3, 4}, 105))
                .Immutable(341, 205));

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
                .Immutable(100, 0)
                .Memory(Combine(bytes, bytes))
                .Immutable(426, 120));

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
                .Immutable(95, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 5, 6, 7, 8, 9})
                .Immutable(436, 110));

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
                .Immutable(536, 10));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtEndOfBuffer()
    {
        var bytes = new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

        _buffer.ValidateWrite(546 - bytes.Length, bytes, expects =>
            expects
                .Length(546)
                .Immutable(536, 0)
                .Memory(bytes));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AfterDelete()
    {
        var bytes = new byte[] {97};

        _buffer.Delete(37, 4);
        _buffer.ValidateWrite(37, bytes, expects =>
            expects
                .Length(542)
                .Immutable(37, 0)
                .Memory(bytes)
                .Immutable(504, 42));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_AtStartReplaceChunk()
    {
        var bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 };
        var expectedBytes = Combine(bytes, bytes);

        _buffer.Write(16, new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1 });
        _buffer.ValidateWrite(0, bytes, expects =>
            expects
                .Length(546)
                .Memory(expectedBytes)
                .Immutable(514, 32));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_ALotOfSingleBytes()
    {
        _buffer.Write(169, new byte[] {160});
        _buffer.Write(169, new byte[] {171});
        _buffer.Write(170, new byte[] {192});
        _buffer.Write(170, new byte[] {205});
        _buffer.Write(171, new byte[] {224});
        _buffer.Write(171, new byte[] {237});
        _buffer.Write(172, new byte[] {240});
        _buffer.Write(172, new byte[] {255});
        _buffer.Write(132, new byte[] {97});
        _buffer.Write(133, new byte[] {97});
        _buffer.Write(134, new byte[] {97});
        _buffer.Write(135, new byte[] {97});
        _buffer.Write(136, new byte[] {97});
        _buffer.Write(137, new byte[] {97});
        _buffer.Write(138, new byte[] {97});
        _buffer.Write(139, new byte[] {97});
        _buffer.Write(140, new byte[] {97});
        _buffer.Write(141, new byte[] {97});
        _buffer.Write(142, new byte[] {97});
        _buffer.Write(143, new byte[] {97});
        _buffer.Write(144, new byte[] {97});
        _buffer.Write(145, new byte[] {97});
        _buffer.Write(146, new byte[] {97});
        _buffer.Write(147, new byte[] {97});
        _buffer.Write(148, new byte[] {97});
        _buffer.Write(149, new byte[] {97});
        _buffer.Write(150, new byte[] {97});
        _buffer.Write(151, new byte[] {97});
        _buffer.Write(152, new byte[] {97});
        _buffer.Write(153, new byte[] {97});
        _buffer.Write(154, new byte[] {97});
        _buffer.Write(155, new byte[] {97});
        _buffer.Write(156, new byte[] {97});
        _buffer.Write(157, new byte[] {97});
        _buffer.Write(158, new byte[] {97});
        _buffer.Write(159, new byte[] {97});
        _buffer.Write(160, new byte[] {97});
        _buffer.Write(161, new byte[] {97});
        _buffer.Write(162, new byte[] {97});
        _buffer.Write(163, new byte[] {97});
        _buffer.Write(164, new byte[] {97});
        _buffer.Write(165, new byte[] {97});

        _buffer.Undo();
    }
}