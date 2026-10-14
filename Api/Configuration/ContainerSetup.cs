using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Api.Configuration
{
    // Opt-in setup for a single API instance managed by the local Compose stack.
    public static class ContainerSetup
    {
        public static void ConfigureSigningKey(IConfiguration configuration)
        {
            if (!string.IsNullOrWhiteSpace(configuration["JWT:Key"])) return;
            var directory = configuration["ContainerSetup:StateDirectory"] ?? "/app/state";
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, "jwt-key");
            if (!File.Exists(path))
            {
                File.WriteAllText(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)));
                if (!OperatingSystem.IsWindows())
                    File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
            }
            configuration["JWT:Key"] = File.ReadAllText(path).Trim();
        }

        public static async Task InitializeAsync(IServiceProvider services, IConfiguration configuration)
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<AppDb>().Database.MigrateAsync();
            using var client = new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
            var ollama = configuration["Ollama:BaseUrl"]!.TrimEnd('/');
            var model = configuration["Ollama:Model"] ?? "nomic-embed-text";
            using var pull = await client.PostAsJsonAsync(ollama + "/api/pull", new { name = model, stream = false });
            pull.EnsureSuccessStatusCode();
            using var embedding = await client.PostAsJsonAsync(ollama + "/api/embeddings", new { model, prompt = "Initialize chatbot vector database" });
            embedding.EnsureSuccessStatusCode();
            using var data = JsonDocument.Parse(await embedding.Content.ReadAsStringAsync());
            var dimension = data.RootElement.GetProperty("embedding").GetArrayLength();
            if (dimension == 0) throw new InvalidOperationException("Ollama returned an empty embedding.");
            var url = configuration["Qdrant:HttpUrl"]!.TrimEnd('/') + "/collections/" + Uri.EscapeDataString(configuration["Qdrant:Collection"] ?? "rag-pipeline");
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    using var existing = await client.GetAsync(url);
                    if (existing.StatusCode == HttpStatusCode.NotFound)
                    {
                        using var created = await client.PutAsJsonAsync(url, new { vectors = new { size = dimension, distance = "Cosine" } });
                        created.EnsureSuccessStatusCode();
                    }
                    else
                    {
                        existing.EnsureSuccessStatusCode();
                        using var collection = JsonDocument.Parse(await existing.Content.ReadAsStringAsync());
                        var vectors = collection.RootElement.GetProperty("result").GetProperty("config").GetProperty("params").GetProperty("vectors");
                        if (vectors.GetProperty("size").GetInt32() != dimension || vectors.GetProperty("distance").GetString() != "Cosine")
                            throw new InvalidOperationException("Existing Qdrant collection does not match the embedding model. Existing data was preserved.");
                    }
                    break;
                }
                catch (HttpRequestException) when (attempt < 29)
                {
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
            }
        }
    }
}
