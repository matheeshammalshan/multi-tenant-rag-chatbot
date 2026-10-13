using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class DocumentEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    
        [ForeignKey(nameof(ChatBot))]
        public int ChatBotId { get; set; }
    
        [MaxLength(100)]
        public string? Name { get; set; }
        public float Size { get; set; }
        public DateOnly UploadDate { get; set; }
    
        [MaxLength(20)]
        public string? Status { get; set; }
        public int Chunks { get; set; }
    
        [MaxLength(255)] 
        public string? FileUrl { get; set; }
    
        public ChatBotEntity? ChatBot{ get; set; }
    }
}