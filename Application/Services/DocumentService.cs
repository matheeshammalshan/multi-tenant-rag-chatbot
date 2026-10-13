using Application.Interfaces.Integrations;
using Application.Interfaces.Observers;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using Application.Observers;
using Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class DocumentService:IDocumentService
    {
        private readonly IDocumentNotifier _notifier;
        private readonly IMapper _mapper;
        private readonly IApplicationRepository _appDb;
        private readonly IFileService _fileService;
        private readonly ILogger<DocumentService> _logger;
        private readonly IChunckCreater _chunkCreater;
        private readonly IEmbeddingsCreater _embeddingsCreater;
        private readonly ISavingEmbeddings _savingEmbeddings;
        private readonly ISubjectObserver _subjectObserver;
        public DocumentService(IMapper mapper, IApplicationRepository appDb,IFileService fileService, 
            ILogger<DocumentService> logger,IChunckCreater  chunkCreater,
            IEmbeddingsCreater embeddingsCreater,ISavingEmbeddings savingEmbeddings,
            ISubjectObserver subjectObserver, IDocumentNotifier notifier)
        {
            _notifier = notifier;
            _mapper = mapper;
            _appDb = appDb;
            _fileService = fileService;
            _logger = logger;
            _chunkCreater = chunkCreater;
            _embeddingsCreater = embeddingsCreater;
            _savingEmbeddings = savingEmbeddings;
            _subjectObserver = subjectObserver;
        }

        public async Task CreateDocument(DocumentDto documentDto)
        {
            var document = _mapper.Map<DocumentDto,DocumentEntity>(documentDto);
       
            using var transaction = await _appDb.BeginTransactionAsync();
        
            DocumentResponseDto responseDto=new DocumentResponseDto();
            responseDto.Message = "document is uploaded";
            responseDto.Status = "Processing";
            await _notifier.NotifyAsync(responseDto);
        
            try
            {
            
                await _appDb.AddAsync(document);
                await _appDb.SaveChangesAsync();
                documentDto.Id = document.Id;
                documentDto.FileUrl=_fileService.CreatePath(documentDto);
                document.FileUrl=documentDto.FileUrl;
                await _fileService.SaveFile(documentDto);
            
                _appDb.Update(document);
                await _appDb.SaveChangesAsync();
            
                string[] chunkList=await _chunkCreater.ChunkCreate(documentDto);

                foreach (var sentense in chunkList)
                {
                    float[]? a = await _embeddingsCreater.CreateEmbeddings(sentense);
                    await _savingEmbeddings.CreateEmbeddingsOnDb(documentDto,a,sentense);
                }
                responseDto.Message = "document is Proceed successfully";
                responseDto.Status = "Proceed";
                responseDto.Chunks=chunkList.Count();
            
                await _notifier.NotifyAsync(responseDto);
            
                document.Status="Proceed";
                document.Chunks=chunkList.Count();

                _appDb.Update(document);
                await _appDb.SaveChangesAsync();

                await _subjectObserver.AddChange(new AnalysisDto(
                    documentDto.ChatBotId,
                    0,
                    1,
                    document.Chunks
                ));
            
                await transaction.CommitAsync();

                _logger.LogInformation($"Document {document.Id} created successfully");

            }
            catch (Exception e)
            {
                await transaction.RollbackAsync();
                _logger.LogError(e, $"error creating document {document.Id}");
                throw;
            }

        }
    
    }
}