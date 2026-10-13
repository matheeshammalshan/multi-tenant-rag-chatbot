using Application.Interfaces.Integrations;
using Application.Interfaces.Services;
using Application.DTOs;
using System.Threading.Tasks;

namespace Application.Services
{
    public class ChunkCreater:IChunckCreater
    {
        private readonly IReaderSelector _readerSelector;

        public ChunkCreater(IReaderSelector readerSelector)
        {
            _readerSelector = readerSelector;
        }

        public Task<string[]> ChunkCreate(DocumentDto documentDto)
        {
            return _readerSelector.GetReader(documentDto.ReaderType)
                .CreateText(documentDto);
        
        }
    }
}