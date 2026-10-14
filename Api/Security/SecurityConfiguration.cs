using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Constants;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace Api.Security
{
    public static class SecurityConfiguration
    {
        public static IServiceCollection AddApiSecurity(this IServiceCollection services, IConfiguration configuration)
        {
            var key = configuration["JWT:Key"];
            if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            {
                throw new InvalidOperationException("Configure JWT:Key with a random secret of at least 32 bytes using API user secrets.");
            }

            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUser, CurrentUser>();
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:Issuer"] ?? "ChatbotApi",
                    ValidAudience = configuration["JWT:Audience"] ?? "ChatbotConsole",
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = ClaimTypes.Role,
                    ValidAlgorithms = new string[] { SecurityAlgorithms.HmacSha256 }
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = async context =>
                    {
                        if (!context.Response.HasStarted)
                        {
                            context.HandleResponse();
                            await Results.Problem(statusCode: 401, detail: "Sign in required.").ExecuteAsync(context.HttpContext);
                        }
                    },
                    OnForbidden = context => Results.Problem(statusCode: 403, detail: "Access denied.").ExecuteAsync(context.HttpContext),
                    OnMessageReceived = context =>
                    {
                        if (!context.Request.Headers.ContainsKey("Authorization") && context.Request.Cookies.TryGetValue("access_token", out var token))
                        {
                            context.Token = token;
                        }

                        return Task.CompletedTask;
                    },
                    OnTokenValidated = async context =>
                    {
                        var p = context.Principal!;
                        if (p.FindFirstValue("token_use") != "access" || !Guid.TryParse(p.FindFirstValue("sid"), out var sid) || !Guid.TryParse(p.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                        {
                            context.Fail("Invalid session.");
                            return;
                        }

                        var session = await context.HttpContext.RequestServices.GetRequiredService<IAuthRepository>().FindSessionAsync(sid);
                        if (session == null || session.UserId != userId || session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow || !session.User.IsActive || session.SecurityStamp != session.User.SecurityStamp || p.FindFirstValue("security_stamp") != session.User.SecurityStamp.ToString() || p.FindFirstValue(ClaimTypes.Role) != session.User.Role || p.FindFirstValue("organization_id") != session.User.OrganizationId?.ToString())
                        {
                            context.Fail("Session expired or revoked.");
                        }
                    }
                };
            });
            services.AddAuthorization(options =>
            {
                options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
                options.AddPolicy("PlatformOwner", policy => policy.RequireRole(Roles.Owner));
            });
            var origins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? new string[]
            {
                "http://localhost:5173"
            };
            services.AddCors(options => options.AddPolicy("Frontend", policy => policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                options.AddPolicy("Auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = Math.Clamp(configuration.GetValue("RateLimits:AuthPermitLimit", 10), 1, 1000), Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
                options.AddPolicy("Chat", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            });
            return services;
        }
    }
}
