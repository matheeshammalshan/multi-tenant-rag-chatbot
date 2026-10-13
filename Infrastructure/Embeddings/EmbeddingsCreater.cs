using Application.Interfaces.Integrations;
using Infrastructure.DTOs;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Infrastructure.Embeddings
{
    public class EmbeddingsCreater:IEmbeddingsCreater
    {
        private readonly HttpClient _client;

        public EmbeddingsCreater(HttpClient client)
        {
            _client = client;
        }

        public async Task<float[]?> CreateEmbeddings(string promt)
        {
            var payload = new
            {
                model = "nomic-embed-text",
                prompt = promt
            };

            var content = new StringContent(
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json"
            );

            HttpResponseMessage response =
                await _client.PostAsync("http://localhost:11434/api/embeddings", content);

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