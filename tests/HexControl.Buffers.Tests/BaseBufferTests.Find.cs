using Xunit;

namespace HexControl.Buffers.Tests;

public partial class ByteBufferTests
{
    [Fact]
    public void Find_Forward()
    {
        _buffer.Write(100, new byte[] {75, 57, 69, 96, 24, 42, 1, 2, 3, 4});
        _buffer.Write(115, new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20});

        var offset = _buffer.Find(new byte[] {75, 57, 69, 96, 24, 42}, 0);
        
        Assert.Equal(100, offset);
    }

    [Fact]
    public void Find_Forward_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });
        
        var offset = _buffer.Find(new byte[] { 75, 57, 69, 96, 24, 42 }, 200);
        
        Assert.Equal(100, offset);
    }

    [Fact]
    public void Find_Forward_NotFound_Without_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var offset = _buffer.Find(new byte[] { 75, 57, 69, 96, 24, 42 }, 200, _buffer.Length - 200);

        Assert.Equal(-1, offset);
    }

    [Fact]
    public void Find_Backward()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var offset = _buffer.Find(new byte[] { 75, 57, 69, 96, 24, 42 }, 200, new FindOptions
        {
            Backward = true
        });

        Assert.Equal(100, offset);
    }

    [Fact]
    public void Find_Backward_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });
        
        var offset = _buffer.Find(new byte[] { 75, 57, 69, 96, 24, 42 }, 50, new FindOptions
        {
            Backward = true
        });
        
        Assert.Equal(100, offset);
    }

    [Fact]
    public void Find_Backward_WrapAround_2()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });
        
        var offset = _buffer.Find(new byte[] { 42, 1, 2, 3, 4 }, 104, new FindOptions
        {
            Backward = true
        });

        Assert.Equal(105, offset);
    }

    [Fact]
    public void FindAll_Forward()
    {
        var actualOffsets = _buffer.FindAll(new byte[] {0x61, 0x62, 0x63, 0x64, 0x65}, 0, _buffer.Length);

        var expectedOffsets = new long[]
            {0, 26, 78, 104, 130, 156, 182, 208, 234, 260, 286, 312, 338, 364, 390, 416, 442, 468, 494, 520};
        
        CompareOffsets(expectedOffsets, actualOffsets);
    }

    [Fact]
    public void FindAll_Forward_WrapAround()
    {
        _buffer.Write(55, new byte[] { 0x64, 0x65, 0x66 });

        var actualOffsets = _buffer.FindAll(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 }, 57, _buffer.Length);

        var expectedOffsets = new long[]
            {78, 104, 130, 156, 182, 208, 234, 260, 286, 312, 338, 364, 390, 416, 442, 468, 494, 520, 0, 26, 52};

        CompareOffsets(expectedOffsets, actualOffsets);
    }

    [Fact]
    public void FindAll_Backward_WrapAround()
    {
        _buffer.Write(55, new byte[] { 0x64, 0x65, 0x66 });
        
        var actualOffset = _buffer.FindAll(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 }, 51, _buffer.Length, new FindOptions
        {
            Backward = true
        });

        var expectedOffsets = new long[]
            {26, 0, 520, 494, 468, 442, 416, 390, 364, 338, 312, 286, 260, 234, 208, 182, 156, 130, 104, 78, 52};

        CompareOffsets(expectedOffsets, actualOffset);
    }

    private static void CompareOffsets(IReadOnlyList<long> expected, IEnumerable<long> actual)
    {
        var actualArray = actual.ToArray();

        Assert.Equal(expected.Count, actualArray.Length);

        for (var i = 0; i < expected.Count; i++)
        {
            Assert.Equal(expected[i], actualArray[i]);
        }
    }
}