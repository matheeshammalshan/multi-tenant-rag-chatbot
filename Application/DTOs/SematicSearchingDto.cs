namespace Application.DTOs
{
    public class SematicSearchingDto
    {
        public int ChatBotId{get;set;}
        public string Query { get; set; } = "";
        public string ApiKey { get; set; } = "";
    }
}