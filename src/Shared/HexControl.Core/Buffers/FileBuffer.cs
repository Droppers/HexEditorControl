using System.IO.MemoryMappedFiles;
using HexControl.Core.Buffers.Chunks;

namespace HexControl.Core.Buffers;

public class FileBuffer : BaseBuffer, IDisposable
{
    private readonly FileStream _fileStream;

    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewStream _stream;
    private readonly MemoryMappedViewAccessor _viewAccessor;

    public FileBuffer(string fileName)
    {
        Filename = fileName;

        _fileStream = File.OpenRead(fileName);

        // TODO: improve handle creation, read/ write , etc
        _memoryMappedFile =
            MemoryMappedFile.CreateFromFile(fileName, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
        _viewAccessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        var length = GetLength(fileName);
        _stream = _memoryMappedFile.CreateViewStream(0, length, MemoryMappedFileAccess.Read);

        var chunk = new FileChunk(this, _fileStream, _memoryMappedFile, _viewAccessor, _stream)
        {
            Length = length
        };
        Init(chunk);
    }

    public string Filename { get; }

    public void Dispose()
    {
        _fileStream.Dispose();
        _stream.Dispose();
        _viewAccessor.Dispose();
        _memoryMappedFile.Dispose();
    }

    private static long GetLength(string fileName) => new FileInfo(fileName).Length;

    protected override long FindInVirtual(IFindStrategy strategy, long startOffset, long length, bool backward) =>
        strategy.SearchInFile(_viewAccessor, startOffset, length, backward);
}