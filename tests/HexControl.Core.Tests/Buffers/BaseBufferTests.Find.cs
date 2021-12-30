using Xunit;

namespace HexControl.Core.Tests.Buffers;

public partial class BaseBufferTests
{
    [Fact]
    public void Find()
    {
        _buffer.Write(100, new byte[] {75, 57, 69, 96, 24, 42, 1, 2, 3, 4});
        _buffer.Write(115, new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 18, 18, 19, 20});

        var offset = _buffer.Find(new byte[] {75, 57, 69, 96, 24, 42}, 0, false, true);
        Assert.Equal(100, offset);

        offset = _buffer.Find(new byte[] {75, 57, 69, 96, 24, 42}, 200, false, true);
        Assert.Equal(100, offset);
    }
}