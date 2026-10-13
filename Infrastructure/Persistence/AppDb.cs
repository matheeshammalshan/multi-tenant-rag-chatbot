using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence
{
    public class AppDb:DbContext
    {
        public AppDb(DbContextOptions<AppDb> options) : base(options)
        {
        }
        public DbSet<OrganizationEntity> Organizations { get; set; }
        public DbSet<ChatBotEntity> ChatBots { get; set; }
        public DbSet<LlmConfigurationEntity> LlmConfig { get; set; }
        public DbSet<DocumentEntity> Document { get; set; }
    
        public DbSet<AnalysisEntity> Analysis { get; set; }
    
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChatBotEntity>()
                .HasOne(e => e.LlmConfiguration)
                .WithOne(l => l.ChatBotEntity)
                .HasForeignKey<LlmConfigurationEntity>(l => l.ChatBotId);
        
            modelBuilder.Entity<ChatBotEntity>()
                .HasOne(e => e.AnalysisEntity)
                .WithOne(a => a.Chat)
                .HasForeignKey<AnalysisEntity>(a => a.ChatBotId);
    
        }

    }
}