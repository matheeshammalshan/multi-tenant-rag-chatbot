using Application.Interfaces.Observers;
using Application.Interfaces.Services;
using Application.Configuration;
using Application.Observers;
using Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(MappingProfile));
            services.AddScoped<IOrganizationService, OrganizationService>();
            services.AddScoped<IChatBotService, ChatBotService>();
            services.AddScoped<ILlmConfigService, LlmConfigService>();
            services.AddScoped<IDocumentService, DocumentService>();
            services.AddScoped<IChunckCreater, ChunkCreater>();
            services.AddSingleton<ISubjectObserver, SubjectObserver>();
            services.AddScoped<ISemanticSearchService, SemanticSearchService>();
            services.AddScoped<IApiKeyService, ApiKeyService>();
            services.AddScoped<IChatHistoryService, ChatHistoryService>();
            return services;
        }
    }
}
