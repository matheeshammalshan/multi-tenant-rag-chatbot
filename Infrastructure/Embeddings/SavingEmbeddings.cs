using Application.Interfaces.Integrations;
using Application.DTOs;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Qdrant.Grpc;

namespace Infrastructure.Embeddings
{
    public class SavingEmbeddings:ISavingEmbeddings
    {
        public async Task CreateEmbeddingsOnDb(DocumentDto document, float[]? embeddings
        ,string sentense)
        {
            var channel = QdrantChannel.ForAddress("http://localhost:6334");
            var client = new QdrantGrpcClient(channel);

            var payload = new Dictionary<string, Value>
            {
                { "chatbotId", new Value { IntegerValue = document.ChatBotId } },
                { "text", new Value { StringValue = sentense } }
            };

            var point = new PointStruct
            {
                Id = new PointId { Uuid = Guid.NewGuid().ToString() },

                Vectors = new Vectors
                {
                    Vector = new Vector { Data = { embeddings } }
                },

                Payload = { payload }
            };

            var upsert = new UpsertPoints()
            {
                CollectionName = "rag-pipeline",
                Points = { point }
            };

            await client.Points.UpsertAsync(upsert);
        }
    }
}