using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class ResourceAccessService : IResourceAccessService
    {
        private readonly ICurrentUser user;
        private readonly IApplicationRepository repository;
        private readonly IConsoleRepository console;
        public ResourceAccessService(ICurrentUser user, IApplicationRepository repository, IConsoleRepository console)
        {
            this.user = user;
            this.repository = repository;
            this.console = console;
        }

        public async Task<ChatBotEntity> RequireChatbotAsync(int id)
        {
            if (user.Id == null)
            {
                throw new ApiException(401, "Sign in required.");
            }

            var bot = await repository.FindChatBotAsync(id);
            if (bot == null || (!user.IsOwner && (bot.OrganizationId != user.OrganizationId || bot.CreatedByUserId != user.Id)))
            {
                throw ApiException.NotFound("Chatbot");
            }

            return bot;
        }

        public async Task RequireOrganizationAsync(int id)
        {
            if (user.Id == null)
            {
                throw new ApiException(401, "Sign in required.");
            }

            if ((!user.IsOwner && user.OrganizationId != id) || !await console.OrganizationExistsAsync(id))
            {
                throw ApiException.NotFound("Organization");
            }
        }

        public void RequireOwner()
        {
            if (user.Id == null)
            {
                throw new ApiException(401, "Sign in required.");
            }

            if (!user.IsOwner)
            {
                throw new ApiException(403, "Platform owner access required.");
            }
        }
    }
}
