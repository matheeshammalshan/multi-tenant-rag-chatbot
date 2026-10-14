using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Persistence
{
    internal static class AppDbConfiguration
    {
        // Matches compose.yaml for local development. Override through configuration.
        private const string DevelopmentConnectionString = "server=localhost;port=3306;user=root;password=1234;database=ragPipeline";
        public static void Configure(DbContextOptionsBuilder options, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("MySql") ?? DevelopmentConnectionString;
            // A fixed server version keeps migration generation independent of a running DB.
            var serverVersion = new MySqlServerVersion(new Version(8, 0, 29));
            options.UseMySql(connectionString, serverVersion, mysql => mysql.MigrationsAssembly(typeof(AppDb).Assembly.GetName().Name));
        }
    }
}
