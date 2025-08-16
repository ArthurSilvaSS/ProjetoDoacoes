using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ProjetoDoacao.Models
{
    public class Campaign
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = string.Empty;

        [Required(ErrorMessage = "A data de início é obrigatória.")]
        public DateTime DataInicio { get; set; }

        public DateTime? DataFim { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal MetaArrecadacao { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ValorArrecadado { get; set; }

        // Chave estrangeira para o criador da campanha
        public int CriadorId { get; set; }
        public User? Criador { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ImagemUrl { get; set; }

        // Relação: Uma campanha pode ter várias doações
        [JsonIgnore]
        public ICollection<Donation> Donations { get; set; } = new List<Donation>();
    }
}
