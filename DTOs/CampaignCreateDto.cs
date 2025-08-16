using System.ComponentModel.DataAnnotations;

namespace ProjetoDoacao.DTOs
{
    public class CampaignCreateDto
    {
        [Required]
        public string Titulo { get; set; } = string.Empty;
        public string Descricao { get; set; } = string.Empty;
        [Required]
        public decimal MetaArrecadacao { get; set; }
        [Required]
        public DateTime DataInicio { get; set; }
        public DateTime? DataFim { get; set; }
        // Propriedade para receber o ficheiro da imagem
        public IFormFile? ImagemArquivo { get; set; }
    }
}