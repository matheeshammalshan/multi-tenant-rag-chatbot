using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Domain.Entities;

namespace Application.Services
{
    public class DocumentService : IDocumentService
    {
        private readonly IApplicationRepository repository;
        private readonly IFileService files;
        private readonly IResourceAccessService access;
        public DocumentService(IApplicationRepository repository, IFileService files, IResourceAccessService access)
        {
            this.repository = repository;
            this.files = files;
            this.access = access;
        }

        public async Task<DocumentResponseDto> CreateDocument(DocumentDto request)
        {
            await access.RequireChatbotAsync(request.ChatBotId);
            var length = request.File.Length;
            if (length == null || length <= 0 || length > 20 * 1024 * 1024)
            {
                throw new ApiException(400, "Upload a non-empty file of at most 20 MB.");
            }

            var name = Path.GetFileName(request.File.FileName.Replace('\\', '/'));
            var extension = Path.GetExtension(name).ToLowerInvariant();
            if (name.Length > 100 || (request.ReaderType == ReaderType.Pdf ? extension != ".pdf" : request.ReaderType != ReaderType.Text || (extension != ".txt" && extension != ".md")))
            {
                throw new ApiException(400, "The filename or extension does not match the selected reader type.");
            }

            var document = new DocumentEntity
            {
                ChatBotId = request.ChatBotId,
                Name = name,
                Size = (float)length / 1048576,
                UploadDate = DateOnly.FromDateTime(DateTime.UtcNow),
                Status = "Processing",
                ReaderType = request.ReaderType.ToString()
            };
            using var transaction = await repository.BeginTransactionAsync();
            string? path = null;
            try
            {
                await repository.AddAsync(document);
                await repository.SaveChangesAsync();
                request.Id = document.Id;
                path = files.CreatePath(request);
                document.FileUrl = path;
                await files.SaveFile(request);
                await repository.AddAsync(new DocumentProcessingJob { DocumentId = document.Id });
                await repository.SaveChangesAsync();
                await transaction.CommitAsync();
                return new DocumentResponseDto()
                {
                    DocumentId = document.Id,
                    ChatBotId = document.ChatBotId,
                    Status = document.Status,
                    Message = "Upload accepted for processing."
                };
            }
            catch
            {
                await transaction.RollbackAsync();
                if (path != null)
                {
                    files.Delete(path);
                }

                throw;
            }
        }
    }
}
