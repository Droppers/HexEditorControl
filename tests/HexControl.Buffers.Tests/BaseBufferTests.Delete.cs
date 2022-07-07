using Xunit;

namespace HexControl.Buffers.Tests;

public partial class BaseBufferTests
{
    [Fact]
    public void Delete_FromVirtualChunk()
    {
        _buffer.ValidateDelete(100, 100, expects =>
            expects
                .Length(446)
                .Immutable(100, 0)
                .Immutable(346, 200));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_FromMemoryChunk()
    {
        _buffer.Write(100, SampleBytes); // ignore

        _buffer.ValidateDelete(102, 5, expects =>
            expects
                .Length(541)
                .Immutable(100, 0)
                .Memory(new byte[] {0, 1, 7, 8, 9})
                .Immutable(436, 110));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_FromEndOfVirtualChunk_And_StartOfMemoryChunk()
    {
        _buffer.Write(100, SampleBytes); // ignore

        _buffer.ValidateDelete(80, 25, expects =>
            expects
                .Length(521)
                .Immutable(80, 0)
                .Memory(new byte[] {5, 6, 7, 8, 9})
                .Immutable(436, 110));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_FromEndOfMemoryChunk_And_StartOfVirtualChunk()
    {
        _buffer.Write(100, SampleBytes); // ignore

        _buffer.ValidateDelete(105, 25, expects =>
            expects
                .Length(521)
                .Immutable(100, 0)
                .Memory(new byte[] {0, 1, 2, 3, 4})
                .Immutable(416, 130));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_MemoryChunkAtStart_Entirely()
    {
        _buffer.Write(0, SampleBytes); // ignore

        _buffer.ValidateDelete(0, 25, expects =>
            expects
                .Length(521)
                .Immutable(521, 25));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_MemoryChunkAtEnd_Entirely()
    {
        _buffer.Write(536, SampleBytes); // ignore

        _buffer.ValidateDelete(526, 20, expects =>
            expects
                .Length(526)
                .Immutable(526, 0));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_AllOfBuffer()
    {
        _buffer.Write(0, SampleBytes); // ignore

        _buffer.ValidateDelete(0, 546, expects =>
            expects
                .Length(0)
                .Memory(Array.Empty<byte>()));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_MultipleSingleByte()
    {
        _buffer.Write(38, new byte[100]); // ignore

        for (var i = 0; i < 10; i++)
        {
            var copyOfI = i;

            _buffer.ValidateDelete(200 - copyOfI, 1, expects =>
                expects
                    .Length(546 - (copyOfI + 1))
                    .Immutable(38, 0)
                    .Memory(new byte[100])
                    .Immutable(62 - copyOfI, 138)
                    .Immutable(345, 201));
        }

        for (var i = 0; i < 10; i++)
        {
            _buffer.ValidateUndo();
        }
    }

    [Fact]
    public void Delete_FirstBlock()
    {
        _buffer.ValidateDelete(16, 1, expects =>
            expects
                .Length(545)
                .Immutable(16, 0)
                .Immutable(529, 17));

        _buffer.ValidateDelete(64, 1, expects =>
            expects
                .Length(544)
                .Immutable(16, 0)
                .Immutable(48, 17)
                .Immutable(480, 66));


        _buffer.ValidateDelete(0, 48, expects =>
            expects
                .Length(496)
                .Immutable(16, 49)
                .Immutable(480, 66));

        _buffer.ValidateUndo();
        _buffer.ValidateUndo();
        _buffer.ValidateUndo();
    }

    // Combined tests
    [Fact]
    public void Delete_WrittenByte()
    {
        _buffer.ValidateWrite(16, 123, expects =>
            expects
                .Length(546)
                .Immutable(16, 0)
                .Memory(new byte[] {123})
                .Immutable(529, 17));

        _buffer.ValidateWrite(64, 123, expects =>
            expects
                .Length(546)
                .Immutable(16, 0)
                .Memory(new byte[] {123})
                .Immutable(47, 17)
                .Memory(new byte[] {123})
                .Immutable(481, 65));

        _buffer.ValidateDelete(10, 48, expects =>
            expects
                .Length(498)
                .Immutable(10, 0)
                .Immutable(6, 58)
                .Memory(new byte[] {123})
                .Immutable(481, 65));

        _buffer.ValidateUndo();
        _buffer.ValidateUndo();
        _buffer.ValidateUndo();
    }

    [Fact]
    public void Delete_AtEnd()
    {
        _buffer.Write(38, new byte[100]);

        _buffer.Insert(_buffer.OriginalLength, new byte[] {160});
        _buffer.Write(_buffer.OriginalLength, new byte[] {171});
        _buffer.Write(_buffer.OriginalLength - 2, new byte[] {171});

        _buffer.ValidateDelete(_buffer.OriginalLength - 2, 1, expects =>
            expects
                .Length(546)
                .Immutable(38, 0)
                .Memory(new byte[100])
                .Immutable(406, 138)
                .Immutable(1, 545)
                .Memory(new byte[] {171}));

        _buffer.ValidateUndo();
    }

    [Fact]
    public void Delete_Undo_PreviouslyWritten()
    {
        _buffer.Write(229, new byte[] {175});
        _buffer.Write(229, new byte[] {171});
        _buffer.Write(230, new byte[] {207});
        _buffer.Write(230, new byte[] {205});


        _buffer.ValidateDelete(229, 1, expects =>
            expects
                .Length(545)
                .Immutable(229, 0)
                .Memory(new byte[] {205})
                .Immutable(315, 231));

        _buffer.ValidateUndoRedo();
    }

    [Fact]
    public void Delete_Undo_TwoSingleByte()
    {
        _buffer.Delete(10, 1);

        _buffer.ValidateDelete(10, 1, expects =>
            expects
                .Length(544)
                .Immutable(10, 0)
                .Immutable(534, 12));

        _buffer.ValidateUndoRedo();
    }
}