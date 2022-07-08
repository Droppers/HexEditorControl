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

        _buffer.ValidateWrite(546, bytes, expects =>
            expects
                .Length(556)
                .Immutable(546, 0)
                .Memory(bytes));
        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Write_ALotOfSingleBytes()
    {
        _buffer.Write(969, new byte[] {160});
        _buffer.Write(969, new byte[] {171});
        _buffer.Write(970, new byte[] {192});
        _buffer.Write(970, new byte[] {205});
        _buffer.Write(971, new byte[] {224});
        _buffer.Write(971, new byte[] {237});
        _buffer.Write(972, new byte[] {240});
        _buffer.Write(972, new byte[] {255});
        _buffer.Write(932, new byte[] {97});
        _buffer.Write(933, new byte[] {97});
        _buffer.Write(934, new byte[] {97});
        _buffer.Write(935, new byte[] {97});
        _buffer.Write(936, new byte[] {97});
        _buffer.Write(937, new byte[] {97});
        _buffer.Write(938, new byte[] {97});
        _buffer.Write(939, new byte[] {97});
        _buffer.Write(940, new byte[] {97});
        _buffer.Write(941, new byte[] {97});
        _buffer.Write(942, new byte[] {97});
        _buffer.Write(943, new byte[] {97});
        _buffer.Write(944, new byte[] {97});
        _buffer.Write(945, new byte[] {97});
        _buffer.Write(946, new byte[] {97});
        _buffer.Write(947, new byte[] {97});
        _buffer.Write(948, new byte[] {97});
        _buffer.Write(949, new byte[] {97});
        _buffer.Write(950, new byte[] {97});
        _buffer.Write(951, new byte[] {97});
        _buffer.Write(952, new byte[] {97});
        _buffer.Write(953, new byte[] {97});
        _buffer.Write(954, new byte[] {97});
        _buffer.Write(955, new byte[] {97});
        _buffer.Write(956, new byte[] {97});
        _buffer.Write(957, new byte[] {97});
        _buffer.Write(958, new byte[] {97});
        _buffer.Write(959, new byte[] {97});
        _buffer.Write(960, new byte[] {97});
        _buffer.Write(961, new byte[] {97});
        _buffer.Write(962, new byte[] {97});
        _buffer.Write(963, new byte[] {97});
        _buffer.Write(964, new byte[] {97});
        _buffer.Write(965, new byte[] {97});

        _buffer.Undo();
    }
}