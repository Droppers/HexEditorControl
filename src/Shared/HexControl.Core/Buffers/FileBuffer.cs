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
    private const int BUFFER_SIZE = 8192;

    private readonly FileStream _fileStream;
    private readonly MemoryMappedFile _memoryMappedFile;
    private readonly MemoryMappedViewAccessor _viewAccessor;

    public FileBuffer(string fileName, FileOpenMode openMode)
    {
        FileName = fileName ?? throw new ArgumentNullException(fileName);

        _fileStream = OpenFileStream(fileName, openMode);

        // Memory mapped file is used for finding byte sequences, especially useful for larger files
        _memoryMappedFile = OpenMemoryMappedFile(_fileStream);
        _viewAccessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);

        Initialize(CreateDefaultChunk());
    }

    public string FileName { get; }

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

    protected sealed override IChunk CreateDefaultChunk() =>
        new FileChunk(this, _fileStream, _viewAccessor)
        {
            Length = _fileStream.Length
        };

    protected override async Task<bool> SaveInternalAsync(CancellationToken cancellationToken)
    {
        // We know that the structure has not changed, therefore we can safely apply all changes
        // to the source stream
        if (!HasStructureChanged())
        {
            var offset = 0L;
            foreach (var chunk in Chunks)
            {
                if (chunk is MemoryChunk memory)
                {
                    _fileStream.Seek(offset, SeekOrigin.Begin);
                    await _fileStream.WriteAsync(memory.Bytes, cancellationToken);
                }

                offset += chunk.Length;
            }

            return true;
        }

        // Save to temporary file since structure has changed
        var tempFileName = Path.GetTempFileName();
        try
        {
            await using var tempStream = new FileStream(tempFileName, FileMode.Open, FileAccess.ReadWrite,
                FileShare.Read, BUFFER_SIZE,
                FileOptions.Asynchronous | FileOptions.SequentialScan);

            var saved = await SaveToFileAsync(tempStream, cancellationToken);
            if (!saved)
            {
                return false;
            }

            // Copy temporary file to actual file
            tempStream.Seek(0, SeekOrigin.Begin);
            _fileStream.Seek(0, SeekOrigin.Begin);
            _fileStream.SetLength(tempStream.Length);
            await tempStream.CopyToAsync(_fileStream, BUFFER_SIZE, cancellationToken);
        }
        finally
        {
            File.Delete(tempFileName);
        }

        return true;
    }

    private FileStream OpenFileStream(string fileName, FileOpenMode openMode)
    {
        try
        {
            IsReadOnly = openMode is FileOpenMode.ReadOnly;
            var fileAccess = IsReadOnly ? FileAccess.Read : FileAccess.ReadWrite;
            return new FileStream(fileName, FileMode.Open, fileAccess, FileShare.Read, BUFFER_SIZE,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
        catch (UnauthorizedAccessException) when (openMode is FileOpenMode.ReadWrite)
        {
            // Could not open in read write mode, attempt to open in readonly mode
            IsReadOnly = true;
            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }

    private static MemoryMappedFile OpenMemoryMappedFile(FileStream fileStream) =>
        MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read,
            HandleInheritability.Inheritable, true);

    protected override long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) =>
        strategy.SearchInFile(_viewAccessor, offset, length, options.Backward);
}