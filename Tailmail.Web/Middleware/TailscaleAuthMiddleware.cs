using Tailmail.Web.Services;

namespace Tailmail.Web.Middleware;

public class TailscaleAuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SettingsService _settingsService;
    private readonly ILogger<TailscaleAuthMiddleware> _logger;

    public TailscaleAuthMiddleware(RequestDelegate next, SettingsService settingsService, ILogger<TailscaleAuthMiddleware> logger)
    {
        _next = next;
        _settingsService = settingsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = _settingsService.GetSettings();
        var allowedUser = settings.AllowedWebUser;

        // Check Tailscale headers
        var tailscaleUser = context.Request.Headers["Tailscale-User-Login"].FirstOrDefault()
                         ?? context.Request.Headers["Tailscale-User-Name"].FirstOrDefault();

        if (string.IsNullOrEmpty(tailscaleUser))
        {
            _logger.LogWarning("Web access without Tailscale headers from {RemoteIp}", context.Connection.RemoteIpAddress);
        }

        // If no allowed user is configured, allow all access
        if (string.IsNullOrEmpty(allowedUser))
        {
            _logger.LogInformation("No allowed web user configured - allowing access");
            await _next(context);
            return;
        }

        if (string.IsNullOrEmpty(tailscaleUser))
        {
            _logger.LogWarning("Access denied: Not accessed via Tailscale");
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync("Access denied: Not accessed via Tailscale");
            return;
        }

        if (!tailscaleUser.Equals(allowedUser, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Access denied: User '{TailscaleUser}' is not authorized (expected '{AllowedUser}')", tailscaleUser, allowedUser);
            context.Response.StatusCode = 403;
            await context.Response.WriteAsync($"Access denied: User '{tailscaleUser}' is not authorized");
            return;
        }

        await _next(context);
    }
}
