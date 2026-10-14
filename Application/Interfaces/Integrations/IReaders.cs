using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IReaders
    {
        Task<string[]> CreateText(DocumentDto doc);
        bool CheckType(ReaderType type);
    }
}
