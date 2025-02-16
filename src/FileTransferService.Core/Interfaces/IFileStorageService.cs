using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransferService.Core.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(string fileName, Stream content, CancellationToken cancellationToken);
    Task<Stream> GetFileAsync(string fileId, CancellationToken cancellationToken);
    Task<IEnumerable<FileInfo>> ListFilesAsync(string folderPath, CancellationToken cancellationToken);
    Task DeleteFileAsync(string fileId, CancellationToken cancellationToken);
}
