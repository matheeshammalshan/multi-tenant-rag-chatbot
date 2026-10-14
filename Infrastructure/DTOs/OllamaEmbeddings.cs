using System.Text.Json.Serialization;

namespace Infrastructure.DTOs
{
    public class OllamaEmbeddings
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = new float[]
        {
        };
    }
}
