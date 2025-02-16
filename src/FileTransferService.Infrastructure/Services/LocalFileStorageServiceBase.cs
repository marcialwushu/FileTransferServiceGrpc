using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FileTransferService.Infrastructure.Services
{
    public class LocalFileStorageServiceBase
    {
        protected readonly string _storageRoot;
        protected readonly ILogger<LocalFileStorageServiceBase> _logger;

        public LocalFileStorageServiceBase(string storageRoot, ILogger<LocalFileStorageServiceBase> logger)
        {
            _storageRoot = storageRoot;
            _logger = logger;

            // Garante que o diretório existe
            Directory.CreateDirectory(_storageRoot);
        }

        /// <summary>
        /// Deleta um arquivo pelo ID.
        /// </summary>
        public async Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(fileId);
            var metadataPath = GetMetadataPath(fileId);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Tentativa de deletar arquivo inexistente: {filePath}");
                return;
            }

            await Task.Run(() => File.Delete(filePath), cancellationToken);
            await Task.Run(() => File.Delete(metadataPath), cancellationToken);

            _logger.LogInformation($"Arquivo {fileId} deletado.");
        }

        /// <summary>
        /// Obtém um arquivo e retorna um Stream.
        /// </summary>
        public async Task<Stream> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var filePath = GetFilePath(fileId);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Arquivo não encontrado: {filePath}");
                throw new FileNotFoundException($"Arquivo com ID {fileId} não encontrado");
            }

            return await Task.FromResult(File.OpenRead(filePath));
        }

        /// <summary>
        /// Lista os arquivos em um diretório.
        /// </summary>
        public async Task<IEnumerable<FileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default)
        {
            var directoryPath = Path.Combine(_storageRoot, folderPath);
            if (!Directory.Exists(directoryPath))
            {
                return Enumerable.Empty<FileInfo>();
            }

            return await Task.Run(() =>
            {
                return new DirectoryInfo(directoryPath)
                    .GetFiles()
                    .ToList();
            }, cancellationToken);
        }

        /// <summary>
        /// Salva um arquivo no armazenamento local.
        /// </summary>
        public async Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var fileId = Guid.NewGuid().ToString();
            var filePath = GetFilePath(fileId);
            var metadataPath = GetMetadataPath(fileId);

            _logger.LogInformation($"Salvando arquivo {fileName} em {filePath}");

            using (var fileStream = File.Create(filePath))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            var metadata = new FileMetadata
            {
                FileName = fileName,
                CreatedAt = DateTime.UtcNow
            };

            await File.WriteAllTextAsync(metadataPath, JsonSerializer.Serialize(metadata), cancellationToken);

            return fileId;
        }

        private string GetFilePath(string fileId) =>
            Path.Combine(_storageRoot, $"{fileId}.dat");

        private string GetMetadataPath(string fileId) =>
            Path.Combine(_storageRoot, $"{fileId}.meta.json");

        private class FileMetadata
        {
            public string FileName { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}