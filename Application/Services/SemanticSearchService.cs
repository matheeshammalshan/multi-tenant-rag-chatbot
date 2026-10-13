using Application.Interfaces.Integrations;
using Application.Interfaces.Repositories;
using Application.Interfaces.Services;
using Application.DTOs;
using AutoMapper;



namespace Application.Services
{
    public class SemanticSearchService:ISemanticSearchService
    {
        private readonly IEmbeddingsCreater _embeddingsCreater;
        private readonly IRetriveEmbeddings _retriveEmbeddings;
        private readonly IApplicationRepository _appDb;
        private readonly IMapper _mapper;
        private readonly IModelSelector _modelSelector;
        private readonly IChatHistoryRepository _historyRepository;
    
        public SemanticSearchService(IEmbeddingsCreater embeddingsCreater,
            IRetriveEmbeddings retriveEmbeddings,IApplicationRepository appDb,IMapper mapper,IModelSelector modelSelector,
            IChatHistoryRepository historyRepository)
        {
          _embeddingsCreater = embeddingsCreater;
          _retriveEmbeddings = retriveEmbeddings;
          _appDb = appDb;
          _mapper = mapper;
          _modelSelector = modelSelector;
          _historyRepository = historyRepository;
        }
    
        public async Task<string> SearchEmbeddings(SematicSearchingDto semanticSearchingDto)
        {
            var chatBotWithApiKey=await _appDb.FindChatBotAsync(semanticSearchingDto.ChatBotId);
            if (chatBotWithApiKey == null)
            {
                return "No api key found";
            }

            if (chatBotWithApiKey.ApiKey != semanticSearchingDto.ApiKey)
            {
                return "Invalid api key";
            }
        
            float[]? embeddings = await _embeddingsCreater.CreateEmbeddings(semanticSearchingDto.Query);

            if (embeddings==null)
            {
                return "No embedding found";
            }
            var configurationEntity = await _appDb.FindConfigurationAsync(semanticSearchingDto.ChatBotId);

            var congifDto=_mapper.Map<LlmConfigurationsDto>(configurationEntity);
        
            string words = await _retriveEmbeddings.RetriveEmbeddingsAsWords(embeddings, 
                 semanticSearchingDto.ChatBotId);
         
            var model=_modelSelector.GetModels(congifDto.ModelName);
        
            string systemMessage=await model.CreateChat(semanticSearchingDto.Query, congifDto, words);
        
            await CreateChatHistory(semanticSearchingDto.ChatBotId,semanticSearchingDto.Query,systemMessage);
        
            return systemMessage; //await model.CreateChat(semanticSearchingDto.Query, congifDto, words);

        }

        public async Task CreateChatHistory(int chatBotId,string userQuery,string systemQuery)
        {   
            await _historyRepository.AddAsync(chatBotId, userQuery, systemQuery);
        }
    
    }
}