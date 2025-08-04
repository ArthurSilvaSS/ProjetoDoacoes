using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProjetoDoacao.Models
{
    public class Donation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Valor { get; set; }

        public DateTime DataDoacao { get; set; }

        // Chave estrangeira para o usuário que doou
        public int UsuarioId { get; set; }
        public User? Doador { get; set; }

        // Chave estrangeira para a campanha que recebeu a doação
        public int CampanhaId { get; set; }
        public Campaign? Campanha { get; set; }
    }
}
