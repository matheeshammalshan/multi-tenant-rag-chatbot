using Application.Interfaces.Integrations;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Qdrant.Grpc;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Embeddings
{
    public class SavingEmbeddings : ISavingEmbeddings
    {
        private readonly IConfiguration configuration;
        public SavingEmbeddings(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task CreateEmbeddingsOnDb(DocumentDto document, float[]? embeddings, string sentense, int chunkIndex = 0)
        {
            using var channel = QdrantChannel.ForAddress(configuration["Qdrant:GrpcUrl"] ?? "http://localhost:6334");
            var client = new QdrantGrpcClient(channel);
            var payload = new Dictionary<string, Value>
            {
                {
                    "chatbotId",
                    new Value
                    {
                        IntegerValue = document.ChatBotId
                    }
                },
                {
                    "documentId",
                    new Value
                    {
                        IntegerValue = document.Id
                    }
                },
                {
                    "chunkIndex",
                    new Value
                    {
                        IntegerValue = chunkIndex
                    }
                },
                {
                    "text",
                    new Value
                    {
                        StringValue = sentense
                    }
                }
            };
            byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes($"document:{document.Id}:chunk:{chunkIndex}"));
            byte[] pointIdBytes = new byte[16];
            Array.Copy(hash, pointIdBytes, pointIdBytes.Length);

            var point = new PointStruct
            {
                Id = new PointId
                {
                    Uuid = new Guid(pointIdBytes).ToString()
                },
                Vectors = new Vectors
                {
                    Vector = new Vector
                    {
                        Data =
                        {
                            embeddings
                        }
                    }
                },
                Payload =
                {
                    payload
                }
            };
            var upsert = new UpsertPoints()
            {
                CollectionName = configuration["Qdrant:Collection"] ?? "rag-pipeline",
                Wait = true,
                Points =
                {
                    point
                }
            };
            await client.Points.UpsertAsync(upsert);
        }
    }
}
