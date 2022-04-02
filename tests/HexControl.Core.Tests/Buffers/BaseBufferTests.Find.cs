using System.Linq;
using HexControl.Core.Buffers;
using Xunit;

namespace HexControl.Core.Tests.Buffers;

public partial class BaseBufferTests
{
    [Fact]
    public void Find_Forward()
    {
        _buffer.Write(100, new byte[] {75, 57, 69, 96, 24, 42, 1, 2, 3, 4});
        _buffer.Write(115, new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20});

        var query = _buffer.Query(new byte[] {75, 57, 69, 96, 24, 42}, new BaseBuffer.FindOptions()
        {
            WrapAround = true,
            Backward = false
        });
        var result = query.Next();

        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void Find_Forward_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var query = _buffer.Query(new byte[] { 75, 57, 69, 96, 24, 42 }, new BaseBuffer.FindOptions()
        {
            WrapAround = true,
            Backward = false,
            StartOffset = 200
        });

        var result = query.Next();

        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void Find_Forward_NotFound_Without_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var query = _buffer.Query(new byte[] { 75, 57, 69, 96, 24, 42 }, new BaseBuffer.FindOptions()
        {
            WrapAround = false,
            Backward = false,
            StartOffset = 200
        });

        var result = query.Next();

        Assert.Null(result);
    }

    [Fact]
    public void Find_Backward()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var query = _buffer.Query(new byte[] { 75, 57, 69, 96, 24, 42 }, new BaseBuffer.FindOptions()
        {
            WrapAround = true,
            Backward = true,
            StartOffset = 200
        });

        var result = query.Next();

        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void Find_Backward_WrapAround()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 });
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var query = _buffer.Query(new byte[] { 75, 57, 69, 96, 24, 42 }, new BaseBuffer.FindOptions()
        {
            WrapAround = true,
            Backward = true,
            StartOffset = 50
        });

        var result = query.Next();

        Assert.NotNull(result);
        Assert.Equal(100, result);
    }

    [Fact]
    public void Find_Backward_WrapAround_2()
    {
        _buffer.Write(100, new byte[] { 75, 57, 69, 96, 24, 42, 1, 2, 3, 4 }); // 110, 111, 112, 113, 114
        _buffer.Write(115, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20 });

        var query = _buffer.Query(new byte[] { 42, 1, 2, 3, 4 }, new BaseBuffer.FindOptions()
        {
            WrapAround = true,
            Backward = true,
            StartOffset = 104
        });

        var result = query.Next();

        Assert.NotNull(result);
        Assert.Equal(105, result);
    }

    [Fact]
    public void Find_Enumerator_Forward()
    {
        var query = _buffer.Query(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 }, new BaseBuffer.FindOptions
        {
            WrapAround = true,
            Backward = false,
            StartOffset = 0
        });


        var expectedOffsets = new long[]
            { 0, 26, 78, 104, 130, 156, 182, 208, 234, 260, 286, 312, 338, 364, 390, 416, 442, 468, 494, 520 };

        var actualOffsets = query.ToArray();

        Assert.Equal(expectedOffsets.Length, actualOffsets.Length);

        for (var i = 0; i < expectedOffsets.Length; i++)
        {

            Assert.Equal(expectedOffsets[i], actualOffsets[i]);
        }
    }

    [Fact]
    public void Find_Enumerator_Forward_WrapAround()
    {
        _buffer.Write(55, new byte[] { 0x64, 0x65, 0x66 });

        var query = _buffer.Query(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 }, new BaseBuffer.FindOptions
        {
            WrapAround = true,
            Backward = false,
            StartOffset = 55
        });

        var expectedOffsets = new long[]
            {78, 104, 130, 156, 182, 208, 234, 260, 286, 312, 338, 364, 390, 416, 442, 468, 494, 520, 0, 26, 52, };

        var actualOffsets = query.ToArray();

        Assert.Equal(expectedOffsets.Length, actualOffsets.Length);

        for (var i = 0; i < expectedOffsets.Length; i++)
        {

            Assert.Equal(expectedOffsets[i], actualOffsets[i]);
        }
    }

    [Fact]
    public void Find_Enumerator_Backward_WrapAround()
    {
        _buffer.Write(55, new byte[] { 0x64, 0x65, 0x66 });

        var query = _buffer.Query(new byte[] { 0x61, 0x62, 0x63, 0x64, 0x65 }, new BaseBuffer.FindOptions
        {
            WrapAround = true,
            Backward = true,
            StartOffset = 55
        });

        var expectedOffsets = new long[]
            { 26, 0, 520, 494, 468, 442, 416, 390, 364, 338, 312, 286, 260, 234, 208, 182, 156, 130, 104, 78, 52 };

        var actualOffsets = query.ToArray();
        
        Assert.Equal(expectedOffsets.Length, actualOffsets.Length);

        for (var i = 0; i < actualOffsets.Length; i++)
        {

            Assert.Equal(expectedOffsets[i], actualOffsets[i]);
        }
    }
}