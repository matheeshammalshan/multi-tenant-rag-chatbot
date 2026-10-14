using System.Security.Claims;
using Application.DTOs.Accounts;
using Application.Exceptions;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Api.Controllers
{
    [ApiController, Route("Auth"), EnableRateLimiting("Auth")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService auth;
        private readonly IWebHostEnvironment environment;
        public AuthController(IAuthService auth, IWebHostEnvironment environment)
        {
            this.auth = auth;
            this.environment = environment;
        }

        [AllowAnonymous, HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            return Session(await auth.RegisterAsync(request));
        }

        [AllowAnonymous, HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            return Session(await auth.LoginAsync(request));
        }

        [AllowAnonymous, HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            var refresh = Request.Cookies["refresh_token"];
            if (string.IsNullOrWhiteSpace(refresh))
            {
                throw new ApiException(401, "Refresh cookie required.");
            }

            return Session(await auth.RefreshAsync(refresh, Request.Headers["X-CSRF-Token"].ToString()));
        }

        [AllowAnonymous, HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var refresh = Request.Cookies["refresh_token"];
            // Refresh validates CSRF even if the access token has expired.
            if (User.Identity?.IsAuthenticated != true && !string.IsNullOrEmpty(refresh))
            {
                refresh = (await auth.RefreshAsync(refresh, Request.Headers["X-CSRF-Token"].ToString())).RefreshToken;
            }

            Guid? sessionId = null;
            Guid parsedSessionId;
            if (Guid.TryParse(User.FindFirstValue("sid"), out parsedSessionId))
            {
                sessionId = parsedSessionId;
            }
            await auth.LogoutAsync(sessionId, refresh);
            ClearCookies();
            return NoContent();
        }

        [HttpGet("me")]
        public async Task<UserDto> Me()
        {
            return await auth.GetUserAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!));
        }

        [AllowAnonymous, HttpPost("forgot-password")]
        public async Task<IActionResult> Forgot(ForgotPasswordRequest request)
        {
            await auth.ForgotPasswordAsync(request.Email);
            return Ok(new { message = "If the account exists, a password reset email has been sent." });
        }

        [AllowAnonymous, HttpPost("reset-password")]
        public async Task<IActionResult> Reset(ResetPasswordRequest request)
        {
            await auth.ResetPasswordAsync(request);
            ClearCookies();
            return NoContent();
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> Change(ChangePasswordRequest request)
        {
            await auth.ChangePasswordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), request);
            ClearCookies();
            return NoContent();
        }

        private IActionResult Session(AuthSessionResult result)
        {
            Response.Cookies.Append("access_token", result.AccessToken, Cookie("/", true, result.RememberMe ? result.AccessExpiresAtUtc : null));
            Response.Cookies.Append("refresh_token", result.RefreshToken, Cookie("/Auth", true, result.RememberMe ? result.RefreshExpiresAtUtc : null));
            Response.Cookies.Append("csrf_token", result.CsrfToken, Cookie("/", false, result.RememberMe ? result.RefreshExpiresAtUtc : null));
            return Ok(new { result.User, result.AccessExpiresAtUtc, result.CsrfToken });
        }

        private CookieOptions Cookie(string path, bool httpOnly, DateTime? expires)
        {
            CookieOptions options = new CookieOptions();
            options.Path = path;
            options.HttpOnly = httpOnly;
            options.Secure = !environment.IsDevelopment();
            options.SameSite = SameSiteMode.Strict;
            if (expires.HasValue)
            {
                options.Expires = new DateTimeOffset(expires.Value);
            }
            return options;
        }

        private void ClearCookies()
        {
            Response.Cookies.Delete("access_token", Cookie("/", true, null));
            Response.Cookies.Delete("refresh_token", Cookie("/Auth", true, null));
            Response.Cookies.Delete("csrf_token", Cookie("/", false, null));
        }
    }
}
