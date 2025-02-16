using FileTransferService.Core.Interfaces;
using FileTransferService.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc(options =>
{
    options.MaxReceiveMessageSize = 5 * 1024 * 1024; // 5MB
    options.MaxSendMessageSize = 5 * 1024 * 1024; // 5MB
});

builder.Services.AddSingleton<IFileStorageService, LocalFileStorageService>();

var app = builder.Build();

app.MapGrpcService<FileTransferService.Api.Services.FileTransferService>();
app.MapGet("/", () => "gRPC File Transfer Service");

app.Run();
