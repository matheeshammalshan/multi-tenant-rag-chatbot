using Application.DTOs;
using Application.Interfaces.Integrations;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.FileStorage
{
    public class FileService : IFileService
    {
        public FileService(IConfiguration configuration)
        {
            this.root = Path.GetFullPath(configuration["Storage:Root"] ?? Path.Combine(AppContext.BaseDirectory, "uploads"));
        }

        private readonly string root;
        public string CreatePath(DocumentDto document)
        {
            if (document.Id <= 0)
            {
                throw new InvalidOperationException("Save the document record before storing its file.");
            }

            var extension = document.ReaderType == ReaderType.Pdf ? ".pdf" : ".txt";
            return Path.Combine(root, document.Id.ToString(), "source" + extension);
        }

        public async Task SaveFile(DocumentDto document)
        {
            var path = CreatePath(document);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await document.File.CopyToAsync(stream);
        }

        public Stream OpenRead(string path)
        {
            return new FileStream(Validate(path), FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public void Delete(string path)
        {
            File.Delete(Validate(path));
        }

        private string Validate(string path)
        {
            var full = Path.GetFullPath(path);
            if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("File path is outside private storage.");
            }

            return full;
        }
    }
}
