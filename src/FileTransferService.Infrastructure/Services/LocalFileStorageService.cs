using FileTransferService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Parquet.Meta;
using System.Text.Json;

namespace FileTransferService.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;
    private readonly ILogger<LocalFileStorageService> _logger;

    public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
    {
        _storageRoot = configuration["Storage:LocalPath"]
            ?? Path.Combine(Path.GetTempPath(), "FileTransferService");
        _logger = logger;

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken)
    {
        var fileId = Guid.NewGuid().ToString();
        var filePath = GetFilePath(fileId);

        using var fileStream = File.Create(filePath);
        await content.CopyToAsync(fileStream, cancellationToken);

        await File.WriteAllTextAsync(
            GetMetadataPath(fileId),
            JsonSerializer.Serialize(new FileMetaData
            {
                //FileName = fileName,
                //CreatedAt = DateTime.UtcNow
            }),
            cancellationToken);

        return fileId;
    }

    public Task<Stream> GetFileAsync(string fileId, CancellationToken cancellationToken)
    {
        var filePath = GetFilePath(fileId);
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File with id {fileId} not found");

        return Task.FromResult<Stream>(File.OpenRead(filePath));
    }

    private string GetFilePath(string fileId) =>
        Path.Combine(_storageRoot, $"{fileId}.dat");

    private string GetMetadataPath(string fileId) =>
        Path.Combine(_storageRoot, $"{fileId}.meta.json");

    public Task<IEnumerable<FileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task DeleteFileAsync(string fileId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
