namespace Domain.Entities
{
    public class UserSession
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public Guid UserId { get; set; }
        public UserAccount User { get; set; } = null !;
        public string RefreshTokenHash { get; set; } = "";
        public string CsrfTokenHash { get; set; } = "";
        public Guid SecurityStamp { get; set; }
        public DateTime ExpiresAtUtc { get; set; }
        public DateTime? RevokedAtUtc { get; set; }
        public bool RememberMe { get; set; }
    }
}
