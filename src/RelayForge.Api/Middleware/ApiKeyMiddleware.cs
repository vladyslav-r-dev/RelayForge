using System.Security.Cryptography;
using System.Text;

namespace RelayForge.Api.Middleware;

public sealed class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly byte[] _keyHash;

    public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        var key = configuration["Security:ApiKey"];
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Security:ApiKey must be configured.");
        _keyHash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.Request.Headers.TryGetValue("X-Api-Key", out var values) ||
            values.Count != 1 || string.IsNullOrEmpty(values[0]) ||
            !CryptographicOperations.FixedTimeEquals(_keyHash,
                SHA256.HashData(Encoding.UTF8.GetBytes(values[0]!))))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized client", context.RequestAborted);
            return;
        }

        await _next(context);
    }
}
