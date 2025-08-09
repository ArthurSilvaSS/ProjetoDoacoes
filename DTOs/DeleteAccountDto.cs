using System.ComponentModel.DataAnnotations;

namespace ProjetoDoacao.DTOs
{
    public class DeleteAccountDto
    {
        [Required]
        public string Password { get; set; } = string.Empty;
    }
}