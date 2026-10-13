using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class AnalysisEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    
        [ForeignKey(nameof(Chat))]
        public int ChatBotId { get; set; }
    
        public int TotalQueries {get; set; }
        public int NumberOfDocuments {get; set; }
        public int TotalChunks {get; set; }
    
        public ChatBotEntity?  Chat{get; set; }

    
    }
}