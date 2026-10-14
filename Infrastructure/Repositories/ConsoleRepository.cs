using Application.DTOs;
using Application.DTOs.Accounts;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class ConsoleRepository : IConsoleRepository
    {
        private readonly AppDb db;
        public ConsoleRepository(AppDb db)
        {
            this.db = db;
        }

        public Task<bool> OrganizationExistsAsync(int id)
        {
            return db.Organizations.AnyAsync(x => x.Id == id);
        }

        public Task<List<OrganizationSummaryDto>> OrganizationsAsync(int? organizationId, int skip, int take)
        {
            return db.Organizations
                .AsNoTracking()
                .Where(x => organizationId == null || x.Id == organizationId)
                .OrderBy(x => x.Id)
                .Skip(skip)
                .Take(take)
                .Select(x => new OrganizationSummaryDto(x.Id, x.Name, x.Email, x.PhoneNumber, db.ChatBots.Count(b => b.OrganizationId == x.Id)))
                .ToListAsync();
        }

        public Task<List<UserDto>> UsersAsync(int skip, int take)
        {
            return db.Users
                .AsNoTracking()
                .OrderBy(x => x.CreatedAtUtc)
                .Skip(skip)
                .Take(take)
                .Select(x => new UserDto(x.Id, x.Name, x.Email, x.PhoneNumber, x.Role, x.OrganizationId, x.Organization == null ? null : x.Organization.Name, x.IsActive, x.CreatedAtUtc, db.ChatBots.Count(b => b.CreatedByUserId == x.Id)))
                .ToListAsync();
        }

        public async Task<List<ChatbotSummaryDto>> ChatbotsAsync(int? organizationId, Guid? userId, int skip, int take)
        {
            var bots = await db.ChatBots
                .AsNoTracking()
                .Include(x => x.LlmConfiguration)
                .Include(x => x.AnalysisEntity)
                .Include(x => x.Documents)
                .Where(x => (organizationId == null || x.OrganizationId == organizationId) && (userId == null || x.CreatedByUserId == userId))
                .OrderBy(x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
            return bots
                .Select(ToDto).ToList();
        }

        public Task<List<DocumentDetailsDto>> DocumentsAsync(int chatbotId, int skip, int take)
        {
            return db.Document
                .AsNoTracking()
                .Where(x => x.ChatBotId == chatbotId)
                .OrderByDescending(x => x.Id)
                .Skip(skip)
                .Take(take)
                .Select(x => new DocumentDetailsDto(x.Id, x.ChatBotId, x.Name, x.Size, x.UploadDate, x.ReaderType, x.Status, x.Chunks, x.Error))
                .ToListAsync();
        }

        public Task<DocumentEntity?> FindDocumentAsync(int id)
        {
            return db.Document.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<DashboardDto> DashboardAsync(int? organizationId, Guid? userId)
        {
            var bots = await ChatbotsAsync(organizationId, userId, 0, int.MaxValue);
            var botIds = db.ChatBots
                .Where(x => (organizationId == null || x.OrganizationId == organizationId) && (userId == null || x.CreatedByUserId == userId))
                .Select(x => x.Id);
            var processing = await db.Document.CountAsync(x => botIds.Contains(x.ChatBotId) && x.Status == "Processing");
            return new DashboardDto(bots.Count, bots.Sum(x => x.TotalQueries), bots.Sum(x => x.DocumentCount), processing, bots.Sum(x => x.TotalChunks), bots);
        }

        public async Task SaveAsync()
        {
            await db.SaveChangesAsync();
        }

        public static ChatbotSummaryDto ToDto(ChatBotEntity b)
        {
            LlmConfigurationsDto? configuration = null;
            if (b.LlmConfiguration != null)
            {
                configuration = new LlmConfigurationsDto(
                    b.Id,
                    b.LlmConfiguration.ModelName,
                    b.LlmConfiguration.Temperature,
                    b.LlmConfiguration.MaxToken,
                    b.LlmConfiguration.TopP);
            }

            int totalQueries = 0;
            if (b.AnalysisEntity != null)
            {
                totalQueries = b.AnalysisEntity.TotalQueries;
            }

            int documentCount = 0;
            int totalChunks = 0;
            if (b.Documents != null)
            {
                documentCount = b.Documents.Count;
                foreach (var document in b.Documents)
                {
                    if (document.Status == "Proceed")
                    {
                        totalChunks = totalChunks + document.Chunks;
                    }
                }
            }

            return new ChatbotSummaryDto(
                b.Id,
                b.OrganizationId,
                b.CreatedByUserId,
                b.Name,
                b.Description,
                b.SystemPromt,
                b.ApiKeyPrefix,
                configuration,
                totalQueries,
                documentCount,
                totalChunks);
        }
    }
}
