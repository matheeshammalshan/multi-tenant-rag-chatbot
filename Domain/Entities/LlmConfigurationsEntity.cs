using Domain.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class LlmConfigurationEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    
        [ForeignKey(nameof(ChatBotEntity))]
        public int ChatBotId { get; set; }
    
        public LlmModelsDto ModelName { get; set; }
        public double Temperature { get; set; }
        public int MaxToken { get; set; }
        public double TopP { get; set; }
    
        public ChatBotEntity? ChatBotEntity { get; set; }
    }
}