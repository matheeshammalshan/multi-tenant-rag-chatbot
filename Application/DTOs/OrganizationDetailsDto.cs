using System.ComponentModel.DataAnnotations;

namespace Application.DTOs
{
    public class OrganizationDetailsDto
    {
        [Required, MaxLength(255)]
        public string? Name { get; set; }

        [EmailAddress, MaxLength(255)]
        public string? Email { get; set; }

        [MaxLength(32)]
        public string? PhoneNumber { get; set; }
    }
}
