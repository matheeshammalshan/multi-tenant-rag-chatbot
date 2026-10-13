using Application.Interfaces.Integrations;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Qdrant.Grpc;

namespace Infrastructure.Embeddings
{
    public class RetriveEmbeddings:IRetriveEmbeddings
    {
        public async Task<string> RetriveEmbeddingsAsWords(float[] queary,int chatbotId)
        {
            string retriveQuery="";
        
            var channel = QdrantChannel.ForAddress("http://localhost:6334");
            var client = new QdrantGrpcClient(channel);

            var search = new SearchPoints
            {
                CollectionName = "rag-pipeline",
                Limit = 5,

                WithPayload = new WithPayloadSelector { Enable = true },
                WithVectors = new WithVectorsSelector { Enable = false },

                Filter = new Filter
                {
                    Must =
                    {
                        new Condition
                        {
                            Field = new FieldCondition
                            {
                                Key = "chatbotId",
                                Match = new Match { Integer = chatbotId }
                            }
                        }
                    }
                }
            };

            search.Vector.AddRange(queary);

            var response = await client.Points.SearchAsync(search);
            List<ScoredPoint> p=response.Result.ToList();

            foreach (var i in p)
            {
                retriveQuery=retriveQuery+i.Payload["text"].StringValue;
            }
            return retriveQuery;
        
        }
    }
}