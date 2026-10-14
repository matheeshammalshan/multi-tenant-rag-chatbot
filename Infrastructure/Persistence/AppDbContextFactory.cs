using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDb>
    {
        public AppDb CreateDbContext(string[] args)
        {
            var apiDirectory = FindApiDirectory();
            var environment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
            var configurationBuilder = new ConfigurationBuilder().SetBasePath(apiDirectory).AddJsonFile("appsettings.json", optional: false).AddJsonFile($"appsettings.{environment}.json", optional: true);
            if (environment == "Development")
            {
                configurationBuilder.AddUserSecrets("chatbot-api-local-development");
            }

            var configuration = configurationBuilder.AddEnvironmentVariables().AddCommandLine(args).Build();
            var options = new DbContextOptionsBuilder<AppDb>();
            AppDbConfiguration.Configure(options, configuration);
            return new AppDb(options.Options);
        }

        private static string FindApiDirectory()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Api.csproj")))
                {
                    return directory.FullName;
                }

                var candidate = Path.Combine(directory.FullName, "Api");
                if (File.Exists(Path.Combine(candidate, "Api.csproj")))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException("Run EF commands from the chatbot solution directory or one of its project directories.");
        }
    }
}
