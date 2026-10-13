namespace Application.DTOs
{
    public class AnalysisDto
    {
        public int ChatBotId {get; set;}
        public int TotalQueries {get; set; }
        public int NumberOfDocuments {get; set; }
        public int TotalChunks {get; set; }

        public AnalysisDto(int chatBotId,int totalQueries, int numberOfDocuments, int totalChunks)
        {
            ChatBotId = chatBotId;
            TotalQueries = totalQueries;
            NumberOfDocuments = numberOfDocuments;
            TotalChunks = totalChunks;
        }
    }
}