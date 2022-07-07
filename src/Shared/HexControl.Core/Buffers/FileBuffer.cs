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

    private FileStream _fileStream = null!;
    private MemoryMappedFile _memoryMappedFile = null!;
    private MemoryMappedViewAccessor _viewAccessor = null!;

    public FileBuffer(string fileName, FileOpenMode openMode)
    {
        FileName = fileName ?? throw new ArgumentNullException(fileName);
        OpenMode = openMode;

        InitializeFile();
        Initialize(CreateDefaultChunk());
    }

    public string FileName { get; }
    public FileOpenMode OpenMode { get; }

    public async ValueTask DisposeAsync()
    {
        await CloseFileAsync();
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    private void InitializeFile()
    {
        _fileStream = OpenFileStream(FileName, OpenMode);

        // Memory mapped file is used for finding byte sequences, especially useful for larger files
        _memoryMappedFile = OpenMemoryMappedFile(_fileStream);
        _viewAccessor = _memoryMappedFile.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
    }

    private async Task CloseFileAsync()
    {
        _viewAccessor.Dispose();
        _memoryMappedFile.Dispose();

        await _fileStream.DisposeAsync();
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
            await using var tempStream = OpenFileStream(tempFileName, FileOpenMode.ReadWrite);

            var saved = await SaveToFileAsync(tempStream, cancellationToken);
            if (!saved)
            {
                return false;
            }

            await CloseFileAsync();
            await tempStream.DisposeAsync();
            File.Move(tempFileName, FileName, true);

            InitializeFile();
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
                FileOptions.Asynchronous);
        }
        catch (UnauthorizedAccessException) when (openMode is FileOpenMode.ReadWrite)
        {
            // Could not open in read write mode, attempt to open in readonly mode
            IsReadOnly = true;
            return new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, BUFFER_SIZE,
                FileOptions.Asynchronous);
        }
    }

    private static MemoryMappedFile OpenMemoryMappedFile(FileStream fileStream) =>
        MemoryMappedFile.CreateFromFile(fileStream, null, 0, MemoryMappedFileAccess.Read,
            HandleInheritability.Inheritable, true);

    protected override long FindInImmutable(IFindStrategy strategy, long offset, long length, FindOptions options,
        CancellationToken cancellationToken) =>
        strategy.SearchInFile(_viewAccessor, offset, length, options.Backward);
}