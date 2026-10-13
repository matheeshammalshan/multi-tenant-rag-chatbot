using Application.Interfaces.Integrations;
using Application.DTOs;
using Domain.Enums;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.LlmModels
{
    public class Llama4Scout:IModels
    {
        private readonly IHttpClientFactory? _httpClientFactory;
        private readonly string _apiKey = "";

        public Llama4Scout(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }
        public bool CheckModelType(LlmModelsDto llmModelsDto)
        {
            return llmModelsDto == LlmModelsDto.Llama4Scout;
        }

        public async Task<string> CreateChat(string userQuery,LlmConfigurationsDto configDto,string words)
        {
            var client = _httpClientFactory.CreateClient();

            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", _apiKey);
        
            var requestBody = new
            {
                messages = new[]
                {
              
                    new
                    {
                        role = "system", content = @"You are a Retrieval-Augmented Generation (RAG) assistant.
                                                You must answer ONLY using the information provided in the CONTEXT section.
                                                Do not use prior knowledge.
                                                Do not hallucinate.
                                                If the answer cannot be found in the context, simply say:
                                                'I don’t know based on the provided context.'"
                    
                    },
                    new
                    {
                        role = "user", content = words
                    
                    },
                    new
                    {
                        role = "system", content = userQuery
                    
                    }
                
                },
                model = "meta-llama/llama-4-scout-17b-16e-instruct",
                temperature = configDto.Temperature,
                max_completion_tokens = configDto.MaxToken,
                top_p = configDto.TopP,
                reasoning_effort="medium",
                stream = true,
                stop = (string?)null
            };

            var json = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                CancellationToken.None
            );

            response.EnsureSuccessStatusCode();
        
            var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);

            var fullText = new StringBuilder();

            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(line)) continue;

                // Each SSE line begins with "data:"
                if (line.StartsWith("data:"))
                {
                    var jsonChunk = line.Substring(5).Trim();

                    if (jsonChunk == "[DONE]") 
                        break;

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(jsonChunk);
                        var delta = doc.RootElement
                            .GetProperty("choices")[0]
                            .GetProperty("delta");

                        if (delta.TryGetProperty("content", out var content))
                        {
                            var text = content.GetString();
                            fullText.Append(text);
                        
                        }
                    }
                    catch
                    {
                        // bad chunk - skip
                    }
                }
            }
       
            return fullText.ToString();
        }
    }
}
