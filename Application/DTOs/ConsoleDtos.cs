using Domain.Enums;

namespace Application.DTOs
{
    public class OrganizationSummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public int ChatbotCount { get; set; }

        public OrganizationSummaryDto(int Id, string? Name, string? Email, string? PhoneNumber, int ChatbotCount)
        {
            this.Id = Id;
            this.Name = Name;
            this.Email = Email;
            this.PhoneNumber = PhoneNumber;
            this.ChatbotCount = ChatbotCount;
        }
    }

    public class ChatbotSummaryDto
    {
        public int Id { get; set; }
        public int OrganizationId { get; set; }
        public Guid? CreatedByUserId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? SystemPrompt { get; set; }
        public string? ApiKeyPrefix { get; set; }
        public LlmConfigurationsDto? Configuration { get; set; }
        public int TotalQueries { get; set; }
        public int DocumentCount { get; set; }
        public int TotalChunks { get; set; }

        public ChatbotSummaryDto(int Id, int OrganizationId, Guid? CreatedByUserId, string? Name, string? Description, string? SystemPrompt, string? ApiKeyPrefix, LlmConfigurationsDto? Configuration, int TotalQueries, int DocumentCount, int TotalChunks)
        {
            this.Id = Id;
            this.OrganizationId = OrganizationId;
            this.CreatedByUserId = CreatedByUserId;
            this.Name = Name;
            this.Description = Description;
            this.SystemPrompt = SystemPrompt;
            this.ApiKeyPrefix = ApiKeyPrefix;
            this.Configuration = Configuration;
            this.TotalQueries = TotalQueries;
            this.DocumentCount = DocumentCount;
            this.TotalChunks = TotalChunks;
        }
    }

    public class DocumentDetailsDto
    {
        public int Id { get; set; }
        public int ChatBotId { get; set; }
        public string? Name { get; set; }
        public float Size { get; set; }
        public DateOnly UploadDate { get; set; }
        public string ReaderType { get; set; }
        public string? Status { get; set; }
        public int Chunks { get; set; }
        public string? Error { get; set; }

        public DocumentDetailsDto(int Id, int ChatBotId, string? Name, float Size, DateOnly UploadDate, string ReaderType, string? Status, int Chunks, string? Error)
        {
            this.Id = Id;
            this.ChatBotId = ChatBotId;
            this.Name = Name;
            this.Size = Size;
            this.UploadDate = UploadDate;
            this.ReaderType = ReaderType;
            this.Status = Status;
            this.Chunks = Chunks;
            this.Error = Error;
        }
    }

    public class DashboardDto
    {
        public int ChatbotCount { get; set; }
        public int TotalQueries { get; set; }
        public int DocumentCount { get; set; }
        public int ProcessingCount { get; set; }
        public int TotalChunks { get; set; }
        public IReadOnlyList<ChatbotSummaryDto> Chatbots { get; set; }

        public DashboardDto(int ChatbotCount, int TotalQueries, int DocumentCount, int ProcessingCount, int TotalChunks, IReadOnlyList<ChatbotSummaryDto> Chatbots)
        {
            this.ChatbotCount = ChatbotCount;
            this.TotalQueries = TotalQueries;
            this.DocumentCount = DocumentCount;
            this.ProcessingCount = ProcessingCount;
            this.TotalChunks = TotalChunks;
            this.Chatbots = Chatbots;
        }
    }

    public class ChatbotAccessDto
    {
        public int ChatBotId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }

        public ChatbotAccessDto(int ChatBotId, string? Name, string? Description)
        {
            this.ChatBotId = ChatBotId;
            this.Name = Name;
            this.Description = Description;
        }
    }

    public class ApiKeyResult
    {
        public int ChatBotId { get; set; }
        public string ApiKey { get; set; }
        public string Prefix { get; set; }

        public ApiKeyResult(int ChatBotId, string ApiKey, string Prefix)
        {
            this.ChatBotId = ChatBotId;
            this.ApiKey = ApiKey;
            this.Prefix = Prefix;
        }
    }

    public class ChatAnswerDto
    {
        public int ChatBotId { get; set; }
        public Guid ConversationId { get; set; }
        public string Answer { get; set; }

        public ChatAnswerDto(int ChatBotId, Guid ConversationId, string Answer)
        {
            this.ChatBotId = ChatBotId;
            this.ConversationId = ConversationId;
            this.Answer = Answer;
        }
    }

    public class ModelDto
    {
        public LlmModelsDto Id { get; set; }
        public string Name { get; set; }
        public string ProviderModel { get; set; }
        public bool Available { get; set; }
        public string? UnavailableReason { get; set; }

        public ModelDto(LlmModelsDto Id, string Name, string ProviderModel, bool Available, string? UnavailableReason)
        {
            this.Id = Id;
            this.Name = Name;
            this.ProviderModel = ProviderModel;
            this.Available = Available;
            this.UnavailableReason = UnavailableReason;
        }
    }
}
