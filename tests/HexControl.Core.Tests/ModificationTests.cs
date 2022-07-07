using HexControl.Core.Buffers;
using HexControl.Core.Buffers.Chunks;
using HexControl.Core.Buffers.History.Changes;
using Xunit;

namespace HexControl.Core.Tests;

public class ModificationTests
{
    private readonly BaseBuffer _buffer;

    public ModificationTests()
    {
        var bytes = File.ReadAllBytes(@"..\..\..\..\..\files\sample.txt");
        _buffer = new MemoryBuffer(bytes);
    }

    [Fact]
    public void Remove_At_Middle()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new RemoveFromMemoryChange(3, 2);
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 6, 7, 8, 9}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Remove_At_Start()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new RemoveFromMemoryChange(0, 2);
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {3, 4, 5, 6, 7, 8, 9}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Remove_At_End()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new RemoveFromMemoryChange(originalBuffer.Length - 2, 2);
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Write_At_Middle()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new WriteToMemoryChange(3, new byte[] {0, 0});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 0, 0, 6, 7, 8, 9}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Write_GrowStart()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new WriteToMemoryChange(-3, new byte[] {5, 4, 3, 2, 1});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {5, 4, 3, 2, 1, 3, 4, 5, 6, 7, 8, 9}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Write_GrowEnd()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new WriteToMemoryChange(chunk.Bytes.Length - 2, new byte[] {5, 4, 3, 2, 1});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 4, 5, 6, 7, 5, 4, 3, 2, 1}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Write_GrowBoth()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new WriteToMemoryChange(-2, new byte[] {3, 2, 1, 2, 3, 4, 3, 2});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {3, 2, 1, 2, 3, 4, 3, 2}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Insert_AtMiddle()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new InsertToMemoryChange(2, new byte[] {3, 2, 1});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 2, 1, 3, 4}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Insert_AtStart()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new InsertToMemoryChange(0, new byte[] {3, 2, 1});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {3, 2, 1, 1, 2, 3, 4}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }

    [Fact]
    public void Insert_AtEnd()
    {
        var originalBuffer = new byte[] {1, 2, 3, 4};

        var chunk = new MemoryChunk(_buffer, originalBuffer.ToArray());
        var mod = new InsertToMemoryChange(4, new byte[] {3, 2, 1});
        mod.Apply(_buffer, null!, chunk);
        Assert.Equal(new byte[] {1, 2, 3, 4, 3, 2, 1}, chunk.Bytes);

        mod.Revert(_buffer, null!, chunk);
        Assert.Equal(originalBuffer, chunk.Bytes);
    }
}