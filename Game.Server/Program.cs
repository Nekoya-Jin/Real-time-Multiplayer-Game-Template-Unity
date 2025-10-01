using MagicOnion.Server;
using MagicOnion.Serialization.MemoryPack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Rooms;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<RoomRegistry>();
builder.Services.AddGrpc();
builder.Services.AddMagicOnion(options =>
{
    options.MessageSerializer = MemoryPackMagicOnionSerializerProvider.Instance;
});
builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(7070, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

var app = builder.Build();

app.MapMagicOnionService();

app.Run();
