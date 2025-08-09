using System.ComponentModel.DataAnnotations;

namespace ProjetoDoacao.DTOs
{
    public class CampaignUpdateDto
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título não pode ter mais de 100 caracteres.")]
        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public DateTime? DataFim { get; set; }

        [Range(1, double.MaxValue, ErrorMessage = "A meta de arrecadação deve ser um valor positivo.")]
        public decimal MetaArrecadacao { get; set; }
    }
}