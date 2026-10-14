using Application.DTOs;
using Application.DTOs.Accounts;
using Domain.Entities;

namespace Application.Interfaces.Repositories
{
    public interface IConsoleRepository
    {
        Task<List<OrganizationSummaryDto>> OrganizationsAsync(int? organizationId, int skip, int take);
        Task<bool> OrganizationExistsAsync(int id);
        Task<List<UserDto>> UsersAsync(int skip, int take);
        Task<List<ChatbotSummaryDto>> ChatbotsAsync(int? organizationId, Guid? userId, int skip, int take);
        Task<List<DocumentDetailsDto>> DocumentsAsync(int chatbotId, int skip, int take);
        Task<DocumentEntity?> FindDocumentAsync(int id);
        Task<DashboardDto> DashboardAsync(int? organizationId, Guid? userId);
        Task SaveAsync();
    }
}
