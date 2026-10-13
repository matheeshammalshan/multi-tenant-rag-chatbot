using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities
{
    public class ChatBotEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
    
        [ForeignKey(nameof(Organization))]
        public int OrganizationId { get; set; }
    
        [MaxLength(255)]
        public string? Name { get; set; }
        [MaxLength(255)]
        public string? Description { get; set; }
        [MaxLength(255)]
        public string? SystemPromt { get; set; }
        [MaxLength(255)]
        public string? ApiKey { get; set; }
    
        public OrganizationEntity? Organization { get; set; }
    
        public LlmConfigurationEntity? LlmConfiguration { get; set; }
    
        public ICollection<DocumentEntity>?  Documents { get; set; }=new List<DocumentEntity>();
    
        public AnalysisEntity? AnalysisEntity { get; set; }
    }
}