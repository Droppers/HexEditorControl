using System;
using System.IO;
using Xunit.Abstractions;

namespace HexControl.Core.Tests.Buffers;

public partial class BaseBufferTests
{
    private static readonly byte[] SampleBytes = {0, 1, 2, 3, 4, 5, 6, 7, 8, 9};

    private readonly ValidatingBuffer _buffer;

    public BaseBufferTests(ITestOutputHelper testOutputHelper)
    {
        var bytes = File.ReadAllBytes(@"..\..\..\..\..\files\sample.txt");
        _buffer = new ValidatingBuffer(bytes, testOutputHelper);
    }

    private static byte[] PadRight(byte[] source, int length)
    {
        var bytes = new byte[length];
        Array.Copy(source, 0, bytes, 0, source.Length);
        return bytes;
    }

    private static byte[] Combine(byte[] left, byte[] right)
    {
        var bytes = new byte[left.Length + right.Length];
        Array.Copy(left, 0, bytes, 0, left.Length);
        Array.Copy(right, 0, bytes, right.Length, right.Length);
        return bytes;
    }
}