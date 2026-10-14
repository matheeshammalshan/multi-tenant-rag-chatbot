using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class SematicSearchingDto
    {
        public int ChatBotId { get; set; }

        [Required, MaxLength(8000)]
        public string Query { get; set; } = "";

        [JsonIgnore]
        public string ApiKey { get; set; } = "";
        public Guid? ConversationId { get; set; }
    }
}
