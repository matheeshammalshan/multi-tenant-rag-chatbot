using Application.Interfaces.Integrations;
using Infrastructure.DTOs;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Application.Exceptions;

namespace Infrastructure.Embeddings
{
    public class EmbeddingsCreater : IEmbeddingsCreater
    {
        private readonly HttpClient _client;
        private readonly IConfiguration _configuration;
        public EmbeddingsCreater(HttpClient client, IConfiguration configuration)
        {
            _client = client;
            _configuration = configuration;
        }

        public async Task<float[]?> CreateEmbeddings(string promt)
        {
            var payload = new
            {
                model = _configuration["Ollama:Model"] ?? "nomic-embed-text",
                prompt = promt
            };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            HttpResponseMessage response = await _client.PostAsync((_configuration["Ollama:BaseUrl"] ?? "http://localhost:11434").TrimEnd('/') + "/api/embeddings", content);
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(502, "Embedding service request failed.");
            }

            string json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<OllamaEmbeddings>(json);
            if (result == null)
            {
                return null;
            }

            return result.Embedding;
        }
    }
}
