using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Integrations
{
    public interface IFileService
    {
        Task SaveFile(DocumentDto documentDto);
        string CreatePath(DocumentDto document);
        Stream OpenRead(string path);
        void Delete(string path);
    }
}
