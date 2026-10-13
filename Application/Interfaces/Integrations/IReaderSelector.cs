using Application.DTOs;

namespace Application.Interfaces.Integrations
{
    public interface IReaderSelector
    {
        IReaders GetReader(ReaderType type);
    }
}