using System.ComponentModel.DataAnnotations;

namespace Application.DTOs.Accounts
{
    public class RegisterRequest
    {
        [Required, MaxLength(255)]
        public string OrganizationName { get; set; } = "";

        [Required, MaxLength(255)]
        public string Name { get; set; } = "";

        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = "";

        [Required, MinLength(8), MaxLength(128)]
        public string Password { get; set; } = "";

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }
        public bool AcceptTerms { get; set; }
    }

    public class LoginRequest
    {
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = "";

        [Required, MaxLength(128)]
        public string Password { get; set; } = "";
        public bool RememberMe { get; set; }
    }

    public class ForgotPasswordRequest
    {
        [Required, EmailAddress, MaxLength(255)]
        public string Email { get; set; } = "";
    }

    public class ResetPasswordRequest
    {
        [Required, MaxLength(128)]
        public string Token { get; set; } = "";

        [Required, MinLength(8), MaxLength(128)]
        public string NewPassword { get; set; } = "";
    }

    public class ChangePasswordRequest
    {
        [Required, MaxLength(128)]
        public string CurrentPassword { get; set; } = "";

        [Required, MinLength(8), MaxLength(128)]
        public string NewPassword { get; set; } = "";
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string Role { get; set; }
        public int? OrganizationId { get; set; }
        public string? OrganizationName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public int ChatbotCount { get; set; }

        public UserDto(Guid Id, string Name, string Email, string? PhoneNumber, string Role, int? OrganizationId, string? OrganizationName, bool IsActive, DateTime CreatedAtUtc, int ChatbotCount)
        {
            this.Id = Id;
            this.Name = Name;
            this.Email = Email;
            this.PhoneNumber = PhoneNumber;
            this.Role = Role;
            this.OrganizationId = OrganizationId;
            this.OrganizationName = OrganizationName;
            this.IsActive = IsActive;
            this.CreatedAtUtc = CreatedAtUtc;
            this.ChatbotCount = ChatbotCount;
        }
    }

    public class AuthSessionResult
    {
        public UserDto User { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public string CsrfToken { get; set; }
        public DateTime AccessExpiresAtUtc { get; set; }
        public DateTime RefreshExpiresAtUtc { get; set; }
        public bool RememberMe { get; set; }

        public AuthSessionResult(UserDto User, string AccessToken, string RefreshToken, string CsrfToken, DateTime AccessExpiresAtUtc, DateTime RefreshExpiresAtUtc, bool RememberMe)
        {
            this.User = User;
            this.AccessToken = AccessToken;
            this.RefreshToken = RefreshToken;
            this.CsrfToken = CsrfToken;
            this.AccessExpiresAtUtc = AccessExpiresAtUtc;
            this.RefreshExpiresAtUtc = RefreshExpiresAtUtc;
            this.RememberMe = RememberMe;
        }
    }
}
