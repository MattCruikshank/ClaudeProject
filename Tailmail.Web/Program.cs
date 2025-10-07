using Microsoft.AspNetCore.Server.Kestrel.Core;
using Tailmail.Web.Components;
using Tailmail.Web.Services;
using Tailmail.Web.Middleware;

// dotnet run -- --settings=matt.json
// sudo tailscale serve --bg 5200

// To test it locally:
//   docker build -t tailmail .
//   docker run -p 8080:8080 tailmail

//   For Docker Hub publishing, you'll need to:
//   1. Create a Docker Hub account at https://hub.docker.com
//   2. Login: docker login
//   3. Tag and push:
//   docker tag tailmail yourusername/tailmail:latest
//   docker push yourusername/tailmail:latest

///////////////////////////////////
/*
 # Matt's instance
  docker run -d --name tailmail-matt -p 8080:8080 -p 8081:8081 tailmail

  # Jacob's instance
  docker run -d --name tailmail-jacob -p 9080:8080 -p 9081:8081 tailmail

  Then access them at:
  - Matt: http://localhost:8080
  - Jacob: http://localhost:9080

  If you want each instance to use different settings files, you can mount them as volumes:

sudo docker run -it --rm --name tailmail-matt \
    -p 8080:8080 -p 8081:8081 \
    -v ~/.tailmail/matt.json:/home/appuser/.tailmail/settings.json \
    tailmail

sudo docker run -it --rm --name tailmail-jacob \
    -p 9080:8080 -p 9081:8081 \
    -v ~/.tailmail/jacob.json:/home/appuser/.tailmail/settings.json \
    tailmail

sudo tailscale serve --bg https+insecure://localhost:8080
sudo tailscale serve --bg https+insecure://localhost:9080

sudo tailscale serve --bg https+insecure://localhost:8080
sudo tailscale serve --bg --https=8081 https+insecure://localhost:8081

 sudo tailscale serve --bg --https=9443 https+insecure://localhost:9080
 sudo tailscale serve --bg --https=9081 https+insecure://localhost:9081

 /////////////////////////////
 
sudo tailscale serve --bg --set-path=/ http://localhost:8080
sudo tailscale serve --bg --https=8081 --set-path=/ http://localhost:8081

sudo tailscale serve --bg --set-path=/ http://localhost:8080
sudo tailscale serve --bg --https=8081 --set-path=/ http://localhost:8081

sudo tailscale serve --bg --https=9443 --set-path=/ http://localhost:9080
sudo tailscale serve --bg --https=9081 --set-path=/ http://localhost:9081

//////////////////////////////

sudo docker build -t tailmail .

# Matt's instance
  sudo docker run -it --rm --name tailmail-matt \
    --cap-add=NET_ADMIN --device=/dev/net/tun \
    --env-file .env \
    -e TS_HOSTNAME=tailmail-matt \
     -e TS_SERVE_HTTPS=false \
    -v ~/.tailmail/matt.json:/home/appuser/.tailmail/settings.json \
    tailmail

  # Jacob's instance
  sudo docker run -it --rm --name tailmail-jacob \
    --cap-add=NET_ADMIN --device=/dev/net/tun \
    --env-file .env \
    -e TS_HOSTNAME=tailmail-jacob \
    -e TS_SERVE_HTTPS=false \
    -v ~/.tailmail/jacob.json:/home/appuser/.tailmail/settings.json \
    tailmail

//////////////////////////////
sudo docker login
sudo docker tag tailmail mattcruikshank/tailmail:latest
sudo docker push mattcruikshank/tailmail:latest
//////////////////////////////
 - In Unraid, go to Docker tab
  - Click Add Container
  - Click Template repositories
  - Add your template URL:
  https://raw.githubusercontent.com/yourusername/unraid-templates/master/tailmail-unraid-template.xml
*/

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
var grpcPort = settings.GrpcPort != 0 ? settings.GrpcPort : 8081;
var httpPort = settings.HttpPort != 0 ? settings.HttpPort : 8080;

// Configure Kestrel to support HTTP/2 without TLS
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(grpcPort, o => o.Protocols = HttpProtocols.Http1AndHttp2);
    options.ListenAnyIP(httpPort, o => o.Protocols = HttpProtocols.Http1AndHttp2);
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
