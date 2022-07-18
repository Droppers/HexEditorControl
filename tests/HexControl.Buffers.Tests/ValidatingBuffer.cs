using HexControl.Buffers.Chunks;
using Xunit;
using Xunit.Abstractions;

namespace HexControl.Buffers.Tests;

public delegate ExpectedBuilder ExpectsDelegate(ExpectedBuilder expects);

// A buffer implementation which validates chunks and undo / redo
public class ValidatingBuffer : MemoryBuffer
{
    private readonly ITestOutputHelper _output;
    private readonly Stack<List<IChunk>> _previousChunks;

    public ValidatingBuffer(byte[] bytes, ITestOutputHelper output) : base(bytes)
    {
        _output = output;
        _previousChunks = new Stack<List<IChunk>>();
    }
    
    public void ValidateWrite(long offset, byte @byte, ExpectsDelegate? expects = null)
    {
        Wrap(() => Write(offset, @byte), expects);
    }

    public void ValidateWrite(long offset, byte[] bytes, ExpectsDelegate? expects = null)
    {
        Wrap(() => Write(offset, bytes), expects);
    }

    public void ValidateInsert(long offset, byte[] bytes, ExpectsDelegate? expects = null)
    {
        Wrap(() => Insert(offset, bytes), expects);
    }

    public void ValidateDelete(long offset, long length, ExpectsDelegate? expects = null)
    {
        Wrap(() => Delete(offset, length), expects);
    }

    private void Wrap(Action modificationAction, ExpectsDelegate? expects)
    {
        PushPrevious();
        modificationAction();

        if (expects != null)
        {
            ValidateExpected(expects);
        }
    }

    private void ValidateExpected(ExpectsDelegate expects)
    {
        var builder = new ExpectedBuilder(this);
        expects(builder);

        Assert.Equal(builder.ExpectedLength, Length);
        CompareChunksToChunks(builder.Chunks);
    }

    public void ValidateUndo()
    {
        var expectedChunks = _previousChunks.Pop();

        Undo();
        CompareChunksToChunks(expectedChunks);
    }

    public void ValidateUndoRedo()
    {
        var previousChunks = CloneChunks();
        var expectedChunks = _previousChunks.Pop();

        Undo();
        CompareChunksToChunks(expectedChunks);

        Redo();
        CompareChunksToChunks(previousChunks);
    }

    private void CompareChunksToChunks(IReadOnlyList<IChunk> expectedChunks)
    {
        try
        {
            var index = 0;
            foreach (var actualChunk in Chunks)
            {
                var expectedChunk = expectedChunks[index];

                Assert.IsType(expectedChunk.GetType(), actualChunk);

                switch (expectedChunk)
                {
                    case IImmutableChunk virtualChunkA when actualChunk is IImmutableChunk virtualChunkB:
                        Assert.Equal(virtualChunkA.Length, virtualChunkB.Length);
                        Assert.Equal(virtualChunkA.SourceOffset, virtualChunkB.SourceOffset);
                        break;
                    case MemoryChunk memoryChunkA when actualChunk is MemoryChunk memoryChunkB:
                        Assert.Equal(memoryChunkA.Length, memoryChunkB.Length);
                        Assert.Equal(memoryChunkA.Bytes, memoryChunkB.Bytes);
                        break;
                }

                index++;

                if (index > expectedChunks.Count)
                {
                    Assert.True(false, $"Expected {expectedChunks.Count} chunks, but got {Chunks.Count} chunks.");
                }
            }
        }
        catch
        {
            PrintExpectedActualComparison(expectedChunks, Chunks.ToList());
            throw;
        }
    }

    private void PrintExpectedActualComparison(IReadOnlyList<IChunk> expectedChunks, IReadOnlyList<IChunk> actualChunks)
    {
        var expected = ConvertChunksToStrings(expectedChunks);
        var actual = ConvertChunksToStrings(actualChunks);

        WriteListsAsColumns(expected, actual);
    }

    private void WriteListsAsColumns(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        var maxWidth = a.Max(str => str.Length);

        _output.WriteLine("");
        _output.WriteLine("Expected".PadRight(maxWidth) + " | Actual");

        var length = Math.Max(a.Count, b.Count);
        for (var i = 0; i < length; i++)
        {
            var left = i >= a.Count ? "" : a[i];
            var right = i >= b.Count ? "" : b[i];

            _output.WriteLine(left.PadRight(maxWidth) + " | " + right);
        }
    }

    private static IReadOnlyList<string> ConvertChunksToStrings(IReadOnlyList<IChunk> chunks)
    {
        var strings = new List<string>();

        var length = 0L;
        foreach (var chunk in chunks)
        {
            length += chunk.Length;

            strings.Add(chunk switch
            {
                MemoryChunk memory =>
                    $"Memory({memory.Bytes.Length})", //$"Memory(new [] {{{string.Join(",", memory.Bytes)}}})",
                FileChunk file => $"Immutable({file.Length}, {file.SourceOffset})",
                IImmutableChunk _ => "Virtual()",
                _ => throw new InvalidOperationException("Chunk type is not supported.")
            });
        }

        strings.Insert(0, $"Length({length})");

        return strings;
    }

    private void PushPrevious()
    {
        _previousChunks.Push(CloneChunks());
    }

    private List<IChunk> CloneChunks()
    {
        return Chunks.Select(chunk => chunk.Clone()).ToList();
    }
}