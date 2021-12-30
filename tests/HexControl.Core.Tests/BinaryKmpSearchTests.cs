using HexControl.Core.Buffers;
using Xunit;

namespace HexControl.Core.Tests;

public class BinaryKmpSearchTests
{
    private readonly byte[] _data = {1, 2, 3, 2, 1, 0, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 3, 2, 1, 0, 13, 14, 15};

    [Fact]
    public void SearchInBuffer_Backward_FromEnd()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.SearchInBuffer(_data, _data.Length - 1, 1000, true);
        Assert.Equal(16, result);
    }

    [Fact]
    public void SearchInBuffer_Backward_FromStartIndex()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.SearchInBuffer(_data, 16, 1000, true);
        Assert.Equal(2, result);
    }

    [Fact]
    public void SearchInBuffer_Backward_MaxSearchLength()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.SearchInBuffer(_data, 13, 10, true);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void SearchInBuffer_Forward_FromStart()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.SearchInBuffer(_data, 0, 1000, false);
        Assert.Equal(2, result);
    }

    [Fact]
    public void SearchInBuffer_Forward_FromStartIndex()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});
        var result = kmp.SearchInBuffer(_data, 3, 1000, false);
        Assert.Equal(16, result);
    }

    [Fact]
    public void SearchInBuffer_Forward_MaxSearchLength()
    {
        var kmp = new KmpFindStrategy(new byte[] {3, 2, 1, 0});

        var result = kmp.SearchInBuffer(_data, 4, 10, false);
        Assert.Equal(-1, result);
    }
}