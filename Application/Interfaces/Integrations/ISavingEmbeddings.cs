using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface ISavingEmbeddings
    {
        Task CreateEmbeddingsOnDb(DocumentDto document, float[]? embeddings, string sentense, int chunkIndex = 0);
    }
}
