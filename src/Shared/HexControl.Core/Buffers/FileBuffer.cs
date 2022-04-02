using System.IO.MemoryMappedFiles;
using HexControl.Core.Buffers.Chunks;
using JetBrains.Annotations;

namespace HexControl.Core.Buffers;

[PublicAPI]
public enum FileOpenMode
{
    ReadOnly,
    ReadWrite
}

[PublicAPI]
public class FileBuffer : BaseBuffer, IDisposable, IAsyncDisposable
{
    private readonly FileStream _fileStream;
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _viewAccessor;

    public FileBuffer(string fileName, FileOpenMode openMode)
    {
        Filename = fileName ?? throw new ArgumentNullException(fileName);

        _fileStream = OpenFileStream(fileName, openMode);

        // Memory mapped file is used for finding byte sequences, especially useful for larger files
        _memoryMappedFile = OpenMemoryMappedFile(_fileStream);
        _viewAccessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        Initialize(new FileChunk(this, _fileStream, _viewAccessor)
        {
            Length = _fileStream.Length
        });
    }

    public string Filename { get; }


    public async ValueTask DisposeAsync()
    {
        _viewAccessor.Dispose();
        _memoryMappedFile.Dispose();
        await _fileStream.DisposeAsync();
    }

    public void Dispose()
    {
        _viewAccessor.Dispose();
        _memoryMappedFile.Dispose();
        _fileStream.Dispose();
    }

    private FileStream OpenFileStream(string fileName, FileOpenMode openMode)
    {
        try
        {
            IsReadOnly = openMode is FileOpenMode.ReadOnly;
            return openMode is FileOpenMode.ReadOnly
                ? File.OpenRead(fileName)
                : File.Open(fileName, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
        }
        catch (UnauthorizedAccessException) when (openMode is FileOpenMode.ReadWrite)
        {
            // Could not open in read write mode, attempt to open in readonly mode
            IsReadOnly = true;
            return File.OpenRead(fileName);
        }
    }

    private MemoryMappedFile OpenMemoryMappedFile(FileStream fileStream) =>
        MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read,
            HandleInheritability.Inheritable, true);

    protected override long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) =>
        strategy.SearchInFile(_viewAccessor, offset, length, options.Backward);
}