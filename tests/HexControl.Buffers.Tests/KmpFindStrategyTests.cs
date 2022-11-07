using HexControl.Buffers.Find;
using Xunit;

namespace HexControl.Buffers.Tests;

public class KmpFindStrategyTests
{
    private readonly byte[] _data = {1, 2, 3, 2, 1, 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 2, 1, 0, 13, 14, 15};

    [Fact]
    public void FindInBuffer_Backward_FromEnd()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.FindInBuffer(_data, _data.Length - 1, 1000, new FindOptions
        {
            Backward = true
        }, default);
        Assert.Equal(16, result);
    }

    [Fact]
    public void FindInBuffer_Backward_FromStartIndex()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.FindInBuffer(_data, 16, 1000, new FindOptions
        {
            Backward = true
        }, default);
        Assert.Equal(2, result);
    }

    [Fact]
    public void FindInBuffer_Backward_MaxSearchLength()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.FindInBuffer(_data, 13, 10, new FindOptions
        {
            Backward = true
        }, default);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void FindInBuffer_Forward_FromStart()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.FindInBuffer(_data, 0, 1000, default, default);
        Assert.Equal(2, result);
    }

    [Fact]
    public void FindInBuffer_Forward_FromStartIndex()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});
        var result = kmp.FindInBuffer(_data, 3, 1000, default, default);
        Assert.Equal(16, result);
    }

    [Fact]
    public void FindInBuffer_Forward_MaxSearchLength()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.FindInBuffer(_data, 4, 10, default, default);
        Assert.Equal(-1, result);
    }
}