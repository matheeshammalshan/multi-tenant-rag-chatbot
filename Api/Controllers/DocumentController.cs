using Application.Interfaces.Services;
using Api.Requests;
using Application.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DocumentController
    {
        private readonly IDocumentService _documentService;
    
        public DocumentController(IDocumentService documentService)
        {
            _documentService = documentService;
        }
    
        [HttpPost("create")]
        public async Task CreateDocument([FromForm]DocumentRequest request)
        {
             using var content = request.File.OpenReadStream();
             await _documentService.CreateDocument(request.ToDto(content));
        }
    }
}