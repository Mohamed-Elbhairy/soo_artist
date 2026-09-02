using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace Soo_artist.Middleware
{
    public class ApiKeyMiddleware
    {
        private readonly RequestDelegate _next;
        private const string ApiKeyHeaderName = "X-API-Key";

        public ApiKeyMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IConfiguration configuration)
        {
            var method = context.Request.Method;

            // Only POST, PUT, and DELETE require authentication
            if (HttpMethods.IsPost(method) || HttpMethods.IsPut(method) || HttpMethods.IsDelete(method))
            {
                var expectedApiKey = configuration["ApiKey"];

                // If no API Key is configured in appsettings, we reject requests for safety (or we could allow if expected is empty, but rejecting is safer)
                if (string.IsNullOrEmpty(expectedApiKey))
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("API Key is not configured on the server.");
                    return;
                }

                if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("API Key was not provided.");
                    return;
                }

                if (expectedApiKey != extractedApiKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    await context.Response.WriteAsync("Unauthorized client: Invalid API Key.");
                    return;
                }
            }

            await _next(context);
        }
    }
}
