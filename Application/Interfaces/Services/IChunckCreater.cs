using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Interfaces.Services
{
    public interface IChunckCreater
    {
        Task<string[]> ChunkCreate(DocumentDto documentDto);
    }
}
