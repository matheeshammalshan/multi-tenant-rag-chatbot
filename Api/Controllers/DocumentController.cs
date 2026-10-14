using Application.DTOs;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Api.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController, Route("Document"), Route("Documents")]
    public class DocumentController : ControllerBase
    {
        private readonly IDocumentService service;
        private readonly IConsoleRepository repository;
        private readonly IResourceAccessService access;
        public DocumentController(IDocumentService service, IConsoleRepository repository, IResourceAccessService access)
        {
            this.service = service;
            this.repository = repository;
            this.access = access;
        }

        [HttpGet]
        public async Task<List<DocumentDetailsDto>> List([FromQuery] int chatBotId, [FromQuery] int skip = 0, [FromQuery] int take = 100)
        {
            await access.RequireChatbotAsync(chatBotId);
            return await repository.DocumentsAsync(chatBotId, Math.Max(0, skip), Math.Clamp(take, 1, 200));
        }

        [HttpGet("{id:int}")]
        public async Task<DocumentDetailsDto> Get(int id)
        {
            var doc = await repository.FindDocumentAsync(id) ?? throw ApiException.NotFound("Document");
            await access.RequireChatbotAsync(doc.ChatBotId);
            return new DocumentDetailsDto(doc.Id, doc.ChatBotId, doc.Name, doc.Size, doc.UploadDate, doc.ReaderType, doc.Status, doc.Chunks, doc.Error);
        }

        [HttpPost("create"), Consumes("multipart/form-data"), RequestSizeLimit(21 * 1024 * 1024)]
        public async Task<IActionResult> Create([FromForm] DocumentRequest request)
        {
            using var content = request.File.OpenReadStream();
            var result = await service.CreateDocument(request.ToDto(content));
            return Accepted($"/Documents/{result.DocumentId}", result);
        }
    }
}
