using Domain.Entities;

namespace Application.Interfaces.Services
{
    public interface IResourceAccessService
    {
        Task<ChatBotEntity> RequireChatbotAsync(int id);
        Task RequireOrganizationAsync(int id);
        void RequireOwner();
    }
}
