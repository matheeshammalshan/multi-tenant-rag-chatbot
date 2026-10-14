using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IRetriveEmbeddings
    {
        Task<string> RetriveEmbeddingsAsWords(float[] queary, int chatbotId);
        Task<string> RetrieveReadyAsync(float[] query, int chatbotId, int[] documentIds)
        {
            return RetriveEmbeddingsAsWords(query, chatbotId);
        }
    }
}
