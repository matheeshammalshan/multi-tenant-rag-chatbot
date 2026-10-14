namespace Domain.Entities
{
    public class UserAccount
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string PasswordHash { get; set; } = "";
        public string Role { get; set; } = Constants.Roles.User;
        public int? OrganizationId { get; set; }
        public OrganizationEntity? Organization { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid SecurityStamp { get; set; } = Guid.NewGuid();
        public int FailedLoginAttempts { get; set; }
        public DateTime? LockoutUntilUtc { get; set; }
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}
