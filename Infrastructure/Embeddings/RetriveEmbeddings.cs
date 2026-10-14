using System.Net.Http.Json;
using System.Text.Json;
using Application.Exceptions;
using Application.Interfaces.Integrations;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Embeddings
{
    public class RetriveEmbeddings : IRetriveEmbeddings
    {
        private readonly IHttpClientFactory clients;
        private readonly IConfiguration configuration;
        public RetriveEmbeddings(IHttpClientFactory clients, IConfiguration configuration)
        {
            this.clients = clients;
            this.configuration = configuration;
        }

        public Task<string> RetriveEmbeddingsAsWords(float[] query, int chatbotId)
        {
            return SearchAsync(query, chatbotId, null);
        }

        public Task<string> RetrieveReadyAsync(float[] query, int chatbotId, int[] documentIds)
        {
            return SearchAsync(query, chatbotId, documentIds);
        }

        private async Task<string> SearchAsync(float[] query, int chatbotId, int[]? documentIds)
        {
            var conditions = new List<object>
            {
                new
                {
                    key = "chatbotId",
                    match = new
                    {
                        value = chatbotId
                    }
                }
            };
            if (documentIds != null)
            {
                conditions.Add(new { key = "documentId", match = new { any = documentIds } });
            }

            var url = (configuration["Qdrant:HttpUrl"] ?? "http://localhost:6333").TrimEnd('/') + "/collections/" + Uri.EscapeDataString(configuration["Qdrant:Collection"] ?? "rag-pipeline") + "/points/search";
            using var response = await clients.CreateClient().PostAsJsonAsync(url, new { vector = query, limit = 5, with_payload = true, filter = new { must = conditions } });
            if (!response.IsSuccessStatusCode)
            {
                throw new ApiException(502, "Vector search failed.");
            }

            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return string.Join("\n", json.RootElement.GetProperty("result").EnumerateArray().Select(x => x.GetProperty("payload").GetProperty("text").GetString()));
        }
    }
}
