using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Infrastructure.DocumentReaders;
using Infrastructure.Embeddings;
using Infrastructure.FileStorage;
using Infrastructure.LlmModels;
using Infrastructure.Observers;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Infrastructure.Auth;
using Infrastructure.Documents;

namespace Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration? configuration = null)
        {
            configuration ??= new ConfigurationBuilder().AddEnvironmentVariables().Build();
            services.TryAddSingleton<IConfiguration>(configuration);
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IReaderSelector, ReaderSelector>();
            services.AddScoped<IReaders, PdfDocumentReader>();
            services.AddScoped<IReaders, TextDocumentReader>();
            services.AddHttpClient();
            services.AddHttpClient<IEmbeddingsCreater, EmbeddingsCreater>(client => client.Timeout = TimeSpan.FromMinutes(2));
            services.AddHttpClient("Groq", client => client.Timeout = TimeSpan.FromMinutes(5));
            services.AddScoped<ISavingEmbeddings, SavingEmbeddings>();
            services.AddScoped<IRetriveEmbeddings, RetriveEmbeddings>();
            services.AddSingleton<DocAndChunksObserver>();
            services.AddScoped<IModels, GptOss20B>();
            services.AddScoped<IModels, GptOss120B>();
            services.AddScoped<IModels, KimiK2>();
            services.AddScoped<IModels, Llama4Scout>();
            services.AddScoped<IModelSelector, ModelSelector>();
            services.AddSingleton<IMongoClient>(_ => new MongoClient(configuration["MongoDB:ConnectionString"] ?? "mongodb://localhost:27017/"));
            services.AddDbContext<AppDb>(options => AppDbConfiguration.Configure(options, configuration));
            services.AddScoped<IApplicationRepository, EfApplicationRepository>();
            services.AddScoped<IChatHistoryRepository, MongoChatHistoryRepository>();
            services.AddScoped<IAuthRepository, AuthRepository>();
            services.AddScoped<IConsoleRepository, ConsoleRepository>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<TokenService>();
            services.AddScoped<IAccountEmailSender, SmtpAccountEmailSender>();
            if (configuration.GetValue("Documents:EnableWorker", true))
            {
                services.AddHostedService<DocumentProcessingWorker>();
            }

            return services;
        }
    }
}
