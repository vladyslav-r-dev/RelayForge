namespace RelayForge.Api.Middleware
{
    public class ApiKeyMiddleware 
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyMiddleware> _logger;

        public ApiKeyMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/events") &&
                !context.Request.Path.StartsWithSegments("/endpoints"))
            {
                await _next(context);
                return;
            }

            if (!context.Request.Headers.TryGetValue("X-Api-Key", out var extractedApiKey))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("API Key is missing");
                _logger.LogWarning("API Key is missing");
                return;
            }

            var apiKey = _configuration["Security:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException("API Key is not configured.");
            }

            if (!string.Equals(
                apiKey,
                extractedApiKey.ToString(),
                StringComparison.Ordinal))
            {
                context.Response.StatusCode = 401;
                await context.Response.WriteAsync("Unauthorized client");
                _logger.LogWarning("Unauthorized client attempted to access with invalid API Key");
                return;
            }

            await _next(context);
        }
    }
}