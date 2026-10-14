using Application.Interfaces.Integrations;
using Application.DTOs;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.DocumentReaders
{
    public class TextDocumentReader : IReaders
    {
        public async Task<string[]> CreateText(DocumentDto doc)
        {
            using var buffer = new MemoryStream();
            await doc.File.CopyToAsync(buffer);
            buffer.Position = 0;
            using var reader = new StreamReader(buffer);
            string text = await reader.ReadToEndAsync();
            string[] sentences = text.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToArray();
            return sentences;
        }

        public bool CheckType(ReaderType type)
        {
            return type == ReaderType.Text;
        }
    }
}
