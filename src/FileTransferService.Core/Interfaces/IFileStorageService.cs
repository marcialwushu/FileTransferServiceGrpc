using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransferService.Core.Interfaces
{
    public interface IFileStorageService
    {
        /// <summary>
        /// Salva um arquivo a partir de um fluxo de bytes.
        /// </summary>
        Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken = default);

        /// <summary>
        /// Salva um arquivo a partir de um array de bytes.
        /// </summary>
        Task<string> SaveFileAsync(string fileName, byte[] bytes, CancellationToken cancellationToken = default);

        /// <summary>
        /// Obtém um arquivo pelo ID, retornando um Stream.
        /// </summary>
        Task<Stream> GetFileAsync(string fileId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lista arquivos dentro de um diretório.
        /// </summary>
        Task<IEnumerable<FileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deleta um arquivo pelo ID.
        /// </summary>
        Task DeleteFileAsync(string fileId, CancellationToken cancellationToken = default);
    }
}




