using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Infrastructure.DocumentReaders;
using Infrastructure.Embeddings;
using Infrastructure.FileStorage;
using Infrastructure.LlmModels;
using Infrastructure.Observers;
using Infrastructure.Persistence;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Infrastructure.Configuration
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IReaderSelector, ReaderSelector>();
            services.AddScoped<IReaders, PdfDocumentReader>();
            services.AddScoped<IReaders, TextDocumentReader>();
            services.AddHttpClient();
            services.AddScoped<IEmbeddingsCreater, EmbeddingsCreater>();
            services.AddScoped<ISavingEmbeddings, SavingEmbeddings>();
            services.AddScoped<IRetriveEmbeddings, RetriveEmbeddings>();
            services.AddSingleton<DocAndChunksObserver>();
            services.AddScoped<IModels, GptOss20B>();
            services.AddScoped<IModels, GptOss120B>();
            services.AddScoped<IModels, KimiK2>();
            services.AddScoped<IModels, Llama4Scout>();
            services.AddScoped<IModelSelector, ModelSelector>();
            services.AddSingleton<IMongoClient>(_ => new MongoClient("mongodb://localhost:27017/"));

            var connectionString = "server=localhost;user=root;password=1234;database=ragPipeline";
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));
            services.AddDbContext<AppDb>(options => options.UseMySql(connectionString, serverVersion));
            services.AddScoped<IApplicationRepository, EfApplicationRepository>();
            services.AddScoped<IChatHistoryRepository, MongoChatHistoryRepository>();
            return services;
        }
    }
}
