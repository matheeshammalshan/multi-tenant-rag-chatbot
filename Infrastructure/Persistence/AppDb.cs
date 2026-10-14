using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDb : DbContext
    {
        public AppDb(DbContextOptions<AppDb> options) : base(options)
        {
        }

        public DbSet<OrganizationEntity> Organizations { get; set; }
        public DbSet<ChatBotEntity> ChatBots { get; set; }
        public DbSet<LlmConfigurationEntity> LlmConfig { get; set; }
        public DbSet<DocumentEntity> Document { get; set; }
        public DbSet<AnalysisEntity> Analysis { get; set; }
        public DbSet<UserAccount> Users { get; set; }
        public DbSet<UserSession> Sessions { get; set; }
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; }
        public DbSet<DocumentProcessingJob> DocumentProcessingJobs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>(b =>
            {
                b.Property(x => x.Name).HasMaxLength(255);
                b.Property(x => x.Email).HasMaxLength(255);
                b.HasIndex(x => x.Email).IsUnique();
                b.Property(x => x.PhoneNumber).HasMaxLength(32);
                b.Property(x => x.PasswordHash).HasMaxLength(512);
                b.Property(x => x.Role).HasMaxLength(16);
                b.HasOne(x => x.Organization).WithMany().HasForeignKey(x => x.OrganizationId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<UserSession>(b =>
            {
                b.Property(x => x.RefreshTokenHash).HasMaxLength(64);
                b.Property(x => x.CsrfTokenHash).HasMaxLength(64);
                b.HasIndex(x => x.RefreshTokenHash).IsUnique();
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });
            modelBuilder.Entity<PasswordResetToken>(b =>
            {
                b.Property(x => x.TokenHash).HasMaxLength(64);
                b.HasIndex(x => x.TokenHash).IsUnique();
                b.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
            });
            modelBuilder.Entity<ChatBotEntity>(b =>
            {
                b.Property(x => x.ApiKeyHash).HasMaxLength(64);
                b.Property(x => x.ApiKeyPrefix).HasMaxLength(16);
                b.HasIndex(x => x.ApiKeyHash).IsUnique();
                b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            });
            modelBuilder.Entity<DocumentEntity>(b =>
            {
                b.Property(x => x.ReaderType).HasMaxLength(16);
                b.Property(x => x.Error).HasMaxLength(512);
            });
            modelBuilder.Entity<DocumentProcessingJob>(b =>
            {
                b.HasIndex(x => x.DocumentId).IsUnique();
                b.HasIndex(x => new { x.CompletedAtUtc, x.AvailableAtUtc, x.LeaseUntilUtc });
                b.HasOne(x => x.Document).WithMany().HasForeignKey(x => x.DocumentId);
            });
            modelBuilder.Entity<ChatBotEntity>().HasOne(e => e.LlmConfiguration).WithOne(l => l.ChatBotEntity).HasForeignKey<LlmConfigurationEntity>(l => l.ChatBotId);
            modelBuilder.Entity<ChatBotEntity>().HasOne(e => e.AnalysisEntity).WithOne(a => a.Chat).HasForeignKey<AnalysisEntity>(a => a.ChatBotId);
        }
    }
}
