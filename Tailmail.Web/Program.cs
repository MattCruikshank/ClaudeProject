using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tailmail.Web.Components;
using Tailmail.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<MessageStore>();
builder.Services.AddSingleton<SettingsService>();

// Load settings to get port configuration
var settingsService = new SettingsService();
var settings = settingsService.GetSettings();
var grpcPort = settings.GrpcPort != 0 ? settings.GrpcPort : 5245;
var httpPort = settings.HttpPort != 0 ? settings.HttpPort : 5246;

// Configure Kestrel to support HTTP/2 without TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(grpcPort, o => o.Protocols = HttpProtocols.Http2);
    options.ListenLocalhost(httpPort, o => o.Protocols = HttpProtocols.Http1);
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapGrpcService<MessageServiceImpl>();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
