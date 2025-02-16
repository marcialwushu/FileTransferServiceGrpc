using FileTransferService.Core.Interfaces;
using FileTransferService.Protos;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;

namespace FileTransferService.Api.Services
{
    public class FileTransferService : FileTransfer.FileTransferBase
    {
        private readonly IFileStorageService _fileStorageService;
        private readonly ILogger<FileTransferService> _logger;

        public FileTransferService(IFileStorageService fileStorageService, ILogger<FileTransferService> logger)
        {
            _fileStorageService = fileStorageService;
            _logger = logger;
        }

        /// <summary>
        /// Faz o upload do arquivo em chunks.
        /// </summary>
        public override async Task<UploadFileResponse> UploadFile(
            IAsyncStreamReader<UploadFileRequest> requestStream, ServerCallContext context)
        {
            FileMetadata? metadata = null;
            using var memoryStream = new MemoryStream();

            await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
            {
                switch (request.DataCase)
                {
                    case UploadFileRequest.DataOneofCase.Metadata:
                        metadata = request.Metadata;
                        _logger.LogInformation($"Recebendo arquivo: {metadata.Filename} ({metadata.ContentType})");
                        break;

                    case UploadFileRequest.DataOneofCase.Chunk:
                        await memoryStream.WriteAsync(request.Chunk.ToByteArray(), context.CancellationToken);
                        break;
                }
            }

            if (metadata == null)
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Nenhum metadado recebido"));
            }

            memoryStream.Position = 0; // Resetar o stream antes de salvar
            string fileId = await _fileStorageService.SaveFileAsync(metadata.Filename, memoryStream, context.CancellationToken);

            return new UploadFileResponse
            {
                FileId = fileId,
                Filename = metadata.Filename,
                Size = memoryStream.Length
            };
        }

        /// <summary>
        /// Faz o download de um arquivo por stream.
        /// </summary>
        public override async Task DownloadFile(
            DownloadFileRequest request, IServerStreamWriter<DownloadFileResponse> responseStream, ServerCallContext context)
        {
            using var fileStream = await _fileStorageService.GetFileAsync(request.FileId, context.CancellationToken);
            if (fileStream == null)
            {
                throw new RpcException(new Status(StatusCode.NotFound, "Arquivo não encontrado"));
            }

            // Enviar os metadados primeiro
            var metadata = new FileMetadata
            {
                Filename = request.FileId, // Não temos o nome real do arquivo aqui
                ContentType = "application/octet-stream",
                FileSize = fileStream.Length
            };

            await responseStream.WriteAsync(new DownloadFileResponse { Metadata = metadata });

            // Enviar os chunks do arquivo
            const int chunkSize = 4096;
            var buffer = new byte[chunkSize];
            int bytesRead;
            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, context.CancellationToken)) > 0)
            {
                await responseStream.WriteAsync(new DownloadFileResponse
                {
                    Chunk = ByteString.CopyFrom(buffer, 0, bytesRead)
                });
            }
        }

        /// <summary>
        /// Lista arquivos dentro de um diretório.
        /// </summary>
        public override async Task<ListFilesResponse> ListFiles(ListFilesRequest request, ServerCallContext context)
        {
            var files = await _fileStorageService.ListFilesAsync(request.FolderPath, context.CancellationToken);

            var response = new ListFilesResponse();
            response.Files.AddRange(files.Select(f => new FileInfo
            {
                FileId = f.FullName,
                Filename = f.Name,
                Size = f.Length,
                ContentType = "application/octet-stream", // Definição genérica, pois não temos essa informação
                CreatedAt = f.CreationTimeUtc.ToString("o")
            }));

            return response;
        }
    }
}
