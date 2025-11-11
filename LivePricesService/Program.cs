using System.Net.WebSockets;
using Microsoft.OpenApi.Models;
using LivePricesService.Services;
using MyWS = LivePricesService.Services.WebSocketManager;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Live Prices API", Version = "v1" });
});

// Add your WebSocket manager and background price fetcher
builder.Services.AddSingleton<IPriceStore, PriceStore>();
builder.Services.AddSingleton<MyWS>();
builder.Services.AddHostedService<BinanceBackgroundService>();

var app = builder.Build();

// Enable Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Enable WebSockets
app.UseWebSockets();
app.MapControllers();

// Resolve WebSocketManager
var wsManager = app.Services.GetRequiredService<MyWS>();

// WebSocket endpoint
app.Map("/ws", async context =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = 400;
        return;
    }

    var socket = await context.WebSockets.AcceptWebSocketAsync();
    var clientId = Guid.NewGuid().ToString();
    await wsManager.AddClientAsync(clientId, socket);

    // Keep the connection alive
    while (socket.State == WebSocketState.Open)
        await Task.Delay(1000);
});

// Optional: Periodically log connected clients
_ = Task.Run(async () =>
{
    while (true)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Connected clients: {wsManager.ConnectedClientCount}");
        await Task.Delay(5000);
    }
});

app.Run();
