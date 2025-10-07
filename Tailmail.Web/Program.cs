using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tailmail.Web.Components;
using Tailmail.Web.Services;
using Tailmail.Web.Middleware;

// dotnet run -- --settings=matt.json
// sudo tailscale serve --bg 5200

var builder = WebApplication.CreateBuilder(args);

// Add command line configuration
builder.Configuration.AddCommandLine(args);

// Get settings file from command line or use default
var settingsFileName = builder.Configuration["settings"];

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<MessageStore>();
builder.Services.AddSingleton<SentMessageStore>();
builder.Services.AddSingleton(new SettingsService(settingsFileName));
builder.Services.AddSingleton<MessageSender>();
builder.Services.AddSingleton<NavigationService>();

// Load settings to get port configuration
var settingsService = new SettingsService(settingsFileName);
var settings = settingsService.GetSettings();
var grpcPort = settings.GrpcPort != 0 ? settings.GrpcPort : 5245;
var httpPort = settings.HttpPort != 0 ? settings.HttpPort : 5246;

// Configure Kestrel to support HTTP/2 without TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, o => o.Protocols = HttpProtocols.Http2);
    options.ListenAnyIP(httpPort, o => o.Protocols = HttpProtocols.Http1);
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

// Apply Tailscale authentication only to web routes
app.UseWhen(
    context => context.Request.Path.StartsWithSegments("/_framework") ||
               context.Request.Path.StartsWithSegments("/_blazor") ||
               context.Request.Path == "/" ||
               !context.Request.ContentType?.Contains("application/grpc") == true,
    appBuilder => appBuilder.UseMiddleware<TailscaleAuthMiddleware>()
);

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
