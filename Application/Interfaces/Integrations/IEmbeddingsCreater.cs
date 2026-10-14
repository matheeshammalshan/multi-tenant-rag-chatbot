using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IEmbeddingsCreater
    {
        Task<float[]?> CreateEmbeddings(string promt);
    }
}
