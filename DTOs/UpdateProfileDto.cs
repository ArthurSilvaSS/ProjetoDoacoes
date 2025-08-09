using System.ComponentModel.DataAnnotations;

namespace ProjetoDoacao.DTOs
{
    public class UpdateProfileDto
    {
        [Required]
        [EmailAddress]
        public string NewEmail { get; set; } = string.Empty;

        [Required]
        public string CurrentPassword { get; set; } = string.Empty;
    }
}