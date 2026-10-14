using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Application.DTOs
{
    public class ChatBotDto
    {
        [Range(1, int.MaxValue)]
        public int OrganizationId { get; set; }

        [Required, MaxLength(255)]
        public string? Name { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [JsonIgnore]
        public string? SystemPromt { get; set; }

        [MaxLength(255)]
        public string? SystemPrompt
        {
            get
            {
                return SystemPromt;
            }

            set
            {
                SystemPromt = value;
            }
        }
    }
}
