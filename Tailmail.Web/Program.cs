using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tailmail.Web.Components;
using Tailmail.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to support HTTP/2 without TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenLocalhost(5245, o => o.Protocols = HttpProtocols.Http2);
    options.ListenLocalhost(5246, o => o.Protocols = HttpProtocols.Http1);
});

// Add services to the container.
builder.Services.AddGrpc();
builder.Services.AddSingleton<MessageStore>();

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
