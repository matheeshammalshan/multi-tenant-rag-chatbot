using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IRetriveEmbeddings
    {

        Task<string> RetriveEmbeddingsAsWords(float[] queary, int chatbotId);
    }
}
