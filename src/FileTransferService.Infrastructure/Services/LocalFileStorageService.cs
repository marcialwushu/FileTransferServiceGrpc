using FileTransferService.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Parquet.Meta;
using System.Text.Json;

namespace FileTransferService.Infrastructure.Services
{
    public class LocalFileStorageService : LocalFileStorageServiceBase, IFileStorageService
    {
        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
            : base(configuration["Storage:LocalPath"] ?? Path.Combine(Path.GetTempPath(), "FileTransferService"), logger)
        {
            Directory.CreateDirectory(_storageRoot);
        }

        /// <summary>
        /// Salva um arquivo usando um Stream.
        /// </summary>
        public async Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_storageRoot, fileName);
            _logger.LogInformation($"Salvando arquivo em: {filePath}");

            using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            {
                await content.CopyToAsync(fileStream, cancellationToken);
            }

            return fileName;
        }

        /// <summary>
        /// Salva um arquivo a partir de um array de bytes.
        /// </summary>
        public async Task<string> SaveFileAsync(string fileName, byte[] bytes, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_storageRoot, fileName);
            _logger.LogInformation($"Salvando arquivo em: {filePath}");

            await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
            return fileName;
        }

        /// <summary>
        /// Obtém um arquivo e retorna um Stream.
        /// </summary>
        public async Task<Stream> GetFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_storageRoot, fileId);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Arquivo não encontrado: {filePath}");
                throw new FileNotFoundException("Arquivo não encontrado", fileId);
            }

            return new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true);
        }

        /// <summary>
        /// Lista arquivos em um diretório.
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
                    .Select(f => new FileInfo(f.FullName))
                    .ToList();
            }, cancellationToken);
        }

        /// <summary>
        /// Deleta um arquivo pelo ID.
        /// </summary>
        public async Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default)
        {
            var filePath = Path.Combine(_storageRoot, fileId);

            if (!File.Exists(filePath))
            {
                _logger.LogWarning($"Tentativa de deletar arquivo inexistente: {filePath}");
                return;
            }

            await Task.Run(() => File.Delete(filePath), cancellationToken);
            _logger.LogInformation($"Arquivo deletado: {filePath}");
        }
    }
}


