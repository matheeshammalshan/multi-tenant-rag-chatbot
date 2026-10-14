using Application.Interfaces.Integrations;
using Application.DTOs;
using System;
using System.Collections.Generic;

namespace Infrastructure.DocumentReaders
{
    public class ReaderSelector : IReaderSelector
    {
        Dictionary<ReaderType, IReaders> _readers = new Dictionary<ReaderType, IReaders>();
        public ReaderSelector(IEnumerable<IReaders> readers)
        {
            foreach (var reader in readers)
            {
                foreach (ReaderType readerType in Enum.GetValues(typeof(ReaderType)))
                {
                    if (reader.CheckType(readerType))
                    {
                        _readers.Add(readerType, reader);
                    }
                }
            }
        }

        public IReaders GetReader(ReaderType type)
        {
            return _readers[type];
        }
    }
}
