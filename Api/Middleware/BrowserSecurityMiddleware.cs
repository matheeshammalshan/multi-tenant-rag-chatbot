using System.Security.Claims;
using Application.Interfaces.Repositories;
using Application.Security;
using Api.Filters;

namespace Api.Middleware
{
    public class BrowserSecurityMiddleware
    {
        private readonly RequestDelegate next;
        private readonly IConfiguration configuration;
        public BrowserSecurityMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            this.next = next;
            this.configuration = configuration;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers.CacheControl = "no-store";
            if (context.Request.Method != "GET" && context.Request.Method != "HEAD" && context.Request.Method != "OPTIONS")
            {
                var origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new string[]
                {
                    "http://localhost:5173"
                };
                var origin = context.Request.Headers.Origin.ToString();
                var sameOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
                if (!string.IsNullOrEmpty(origin) && !origin.Equals(sameOrigin, StringComparison.OrdinalIgnoreCase) && !origins.Contains(origin, StringComparer.OrdinalIgnoreCase))
                {
                    await Results.Problem(statusCode: 403, detail: "Origin is not allowed.").ExecuteAsync(context);
                    return;
                }

                if (context.User.Identity?.IsAuthenticated == true && !context.Request.Headers.ContainsKey("Authorization") && context.Request.Cookies.ContainsKey("access_token") && context.GetEndpoint()?.Metadata.GetMetadata<RequiredApiKeyAttribute>()== null)
                {
                    var id = Guid.Parse(context.User.FindFirstValue("sid")!);
                    var session = await context.RequestServices.GetRequiredService<IAuthRepository>().FindSessionAsync(id);
                    if (!SecretTokens.Matches(context.Request.Headers["X-CSRF-Token"].ToString(), session?.CsrfTokenHash))
                    {
                        await Results.Problem(statusCode: 403, detail: "A valid X-CSRF-Token header is required.").ExecuteAsync(context);
                        return;
                    }
                }
            }

            await next(context);
        }
    }
}
