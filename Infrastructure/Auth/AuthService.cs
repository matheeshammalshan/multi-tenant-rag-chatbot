using Application.DTOs.Accounts;
using Application.Exceptions;
using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.Security;
using Domain.Constants;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Infrastructure.Auth
{
    public class AuthService : IAuthService
    {
        private readonly IAuthRepository repository;
        private readonly TokenService tokens;
        private readonly IAccountEmailSender emailSender;
        private readonly AppDb db;
        public AuthService(IAuthRepository repository, TokenService tokens, IAccountEmailSender emailSender, AppDb db)
        {
            this.repository = repository;
            this.tokens = tokens;
            this.emailSender = emailSender;
            this.db = db;
        }

        private readonly PasswordHasher<UserAccount> hasher = new PasswordHasher<UserAccount>(Options.Create(new PasswordHasherOptions { IterationCount = 210000 }));
        private static readonly UserAccount DummyUser = new UserAccount();
        private static readonly string DummyHash = new PasswordHasher<UserAccount>(Options.Create(new PasswordHasherOptions { IterationCount = 210000 })).HashPassword(DummyUser, "Dummy-credential-for-timing-only-941!");
        public async Task<AuthSessionResult> RegisterAsync(RegisterRequest request)
        {
            ValidatePassword(request.Password);
            if (!request.AcceptTerms)
            {
                throw new ApiException(400, "Accept the terms to register.");
            }

            if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.OrganizationName))
            {
                throw new ApiException(400, "Name and organization are required.");
            }

            var email = Normalize(request.Email);
            if (await repository.FindUserByEmailAsync(email)!= null)
            {
                throw new ApiException(409, "An account already uses this email.");
            }

            var user = new UserAccount
            {
                Name = request.Name.Trim(),
                Email = email,
                PhoneNumber = request.PhoneNumber,
                Organization = new OrganizationEntity
                {
                    Name = request.OrganizationName.Trim(),
                    Email = email,
                    PhoneNumber = request.PhoneNumber
                },
                Role = Roles.User
            };
            user.PasswordHash = hasher.HashPassword(user, request.Password);
            await repository.AddAsync(user);
            await repository.SaveAsync();
            return await StartSessionAsync(user, false);
        }

        public async Task<AuthSessionResult> LoginAsync(LoginRequest request)
        {
            var user = await repository.FindUserByEmailAsync(Normalize(request.Email));
            if (user == null)
            {
                hasher.VerifyHashedPassword(DummyUser, DummyHash, request.Password);
                throw new ApiException(401, "Invalid email or password.");
            }

            if (!user.IsActive || user.LockoutUntilUtc > DateTime.UtcNow)
            {
                throw new ApiException(401, "Invalid email or password.");
            }

            var verified = hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
            if (verified == PasswordVerificationResult.Failed)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE `Users` SET `LockoutUntilUtc` = CASE WHEN `FailedLoginAttempts` + 1 >= 5 THEN {DateTime.UtcNow.AddMinutes(15)} ELSE `LockoutUntilUtc` END, `FailedLoginAttempts` = `FailedLoginAttempts` + 1 WHERE `Id` = {user.Id}");
                throw new ApiException(401, "Invalid email or password.");
            }

            if (verified == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = hasher.HashPassword(user, request.Password);
            }

            user.FailedLoginAttempts = 0;
            user.LockoutUntilUtc = null;
            await repository.SaveAsync();
            return await StartSessionAsync(user, request.RememberMe);
        }

        public async Task<AuthSessionResult> RefreshAsync(string refreshToken, string csrfToken)
        {
            var session = await repository.FindRefreshSessionAsync(SecretTokens.Hash(refreshToken));
            if (session == null || session.RevokedAtUtc != null || session.ExpiresAtUtc <= DateTime.UtcNow || !session.User.IsActive || session.SecurityStamp != session.User.SecurityStamp || !SecretTokens.Matches(csrfToken, session.CsrfTokenHash))
            {
                throw new ApiException(401, "Session expired or invalid.");
            }

            var nextRefresh = SecretTokens.Generate();
            var nextCsrf = SecretTokens.Generate();
            if (!await repository.RotateSessionAsync(session.Id, session.RefreshTokenHash, SecretTokens.Hash(nextRefresh), SecretTokens.Hash(nextCsrf)))
            {
                throw new ApiException(401, "Refresh token already used.");
            }

            return Result(session.User, session, nextRefresh, nextCsrf);
        }

        public async Task LogoutAsync(Guid? sessionId, string? refreshToken)
        {
            var session = sessionId is Guid id ? await repository.FindSessionAsync(id) : string.IsNullOrWhiteSpace(refreshToken) ? null : await repository.FindRefreshSessionAsync(SecretTokens.Hash(refreshToken));
            if (session != null)
            {
                session.RevokedAtUtc = DateTime.UtcNow;
                await repository.SaveAsync();
            }
        }

        public async Task<UserDto> GetUserAsync(Guid userId)
        {
            return ToDto(await repository.FindUserAsync(userId) ?? throw ApiException.NotFound("User"));
        }

        public async Task ForgotPasswordAsync(string email)
        {
            var user = await repository.FindUserByEmailAsync(Normalize(email));
            if (user == null || !user.IsActive)
            {
                return;
            }

            var raw = SecretTokens.Generate();
            await repository.AddAsync(new PasswordResetToken { UserId = user.Id, TokenHash = SecretTokens.Hash(raw), ExpiresAtUtc = DateTime.UtcNow.AddMinutes(30) });
            await repository.SaveAsync();
            await emailSender.SendPasswordResetAsync(user.Email, raw);
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            ValidatePassword(request.NewPassword);
            await using var transaction = await db.Database.BeginTransactionAsync();
            var reset = await repository.FindResetAsync(SecretTokens.Hash(request.Token));
            if (reset == null || reset.UsedAtUtc != null || reset.ExpiresAtUtc <= DateTime.UtcNow || !reset.User.IsActive)
            {
                throw new ApiException(400, "Reset token expired or invalid.");
            }

            if (!await repository.ConsumeResetAsync(reset.Id))
            {
                throw new ApiException(400, "Reset token already used.");
            }

            reset.User.PasswordHash = hasher.HashPassword(reset.User, request.NewPassword);
            reset.User.SecurityStamp = Guid.NewGuid();
            reset.User.FailedLoginAttempts = 0;
            reset.User.LockoutUntilUtc = null;
            await repository.RevokeSessionsAsync(reset.UserId);
            await db.PasswordResetTokens.Where(x => x.UserId == reset.UserId && x.UsedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow));
            await repository.SaveAsync();
            await transaction.CommitAsync();
        }

        public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            ValidatePassword(request.NewPassword);
            var user = await repository.FindUserAsync(userId) ?? throw ApiException.NotFound("User");
            if (hasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword) == PasswordVerificationResult.Failed)
            {
                throw new ApiException(400, "Current password is incorrect.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync();
            user.PasswordHash = hasher.HashPassword(user, request.NewPassword);
            user.SecurityStamp = Guid.NewGuid();
            await repository.RevokeSessionsAsync(user.Id);
            await db.PasswordResetTokens.Where(x => x.UserId == user.Id && x.UsedAtUtc == null).ExecuteUpdateAsync(s => s.SetProperty(x => x.UsedAtUtc, DateTime.UtcNow));
            await repository.SaveAsync();
            await transaction.CommitAsync();
        }

        public async Task BootstrapOwnerAsync(string email, string password, string name)
        {
            ValidatePassword(password);
            if (await db.Users.AnyAsync(x => x.Role == Roles.Owner))
            {
                throw new ApiException(409, "A platform owner already exists.");
            }

            if (await repository.FindUserByEmailAsync(Normalize(email))!= null)
            {
                throw new ApiException(409, "Email already registered.");
            }

            var owner = new UserAccount
            {
                Email = Normalize(email),
                Name = name,
                Role = Roles.Owner
            };
            owner.PasswordHash = hasher.HashPassword(owner, password);
            await repository.AddAsync(owner);
            await repository.SaveAsync();
        }

        private async Task<AuthSessionResult> StartSessionAsync(UserAccount user, bool rememberMe)
        {
            var refresh = SecretTokens.Generate();
            var csrf = SecretTokens.Generate();
            var session = new UserSession
            {
                UserId = user.Id,
                RefreshTokenHash = SecretTokens.Hash(refresh),
                CsrfTokenHash = SecretTokens.Hash(csrf),
                SecurityStamp = user.SecurityStamp,
                ExpiresAtUtc = DateTime.UtcNow.AddDays(rememberMe ? 14 : 1),
                RememberMe = rememberMe
            };
            await repository.AddAsync(session);
            await repository.SaveAsync();
            return Result(user, session, refresh, csrf);
        }

        private AuthSessionResult Result(UserAccount user, UserSession session, string refresh, string csrf)
        {
            var expires = DateTime.UtcNow.AddMinutes(15);
            return new AuthSessionResult(ToDto(user), tokens.Create(user, session, expires), refresh, csrf, expires, session.ExpiresAtUtc, session.RememberMe);
        }

        public static UserDto ToDto(UserAccount user)
        {
            return new UserDto(user.Id, user.Name, user.Email, user.PhoneNumber, user.Role, user.OrganizationId, user.Organization?.Name, user.IsActive, user.CreatedAtUtc, 0);
        }

        private static string Normalize(string email)
        {
            return email.Trim().ToLowerInvariant();
        }

        private static void ValidatePassword(string password)
        {
            if (password.Length < 8 || password.Length > 128 || !password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit))
            {
                throw new ApiException(400, "Password must be 8–128 characters with uppercase, lowercase, and a number.");
            }
        }
    }
}
