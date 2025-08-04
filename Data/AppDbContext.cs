// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using ProjetoDoacao.Models;

namespace CampanhaDoacaoAPI.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Donation> Donations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurando a relação entre User e Campaign
            modelBuilder.Entity<User>()
                .HasMany(u => u.Campaigns)
                .WithOne(c => c.Criador)
                .HasForeignKey(c => c.CriadorId)
                .OnDelete(DeleteBehavior.Cascade); // Se um usuário for deletado, suas campanhas também serão.

            // Configurando a relação entre Campaign e Donation
            modelBuilder.Entity<Campaign>()
               .HasMany(c => c.Donations)
               .WithOne(d => d.Campanha)
               .HasForeignKey(d => d.CampanhaId);

            // Configurando a relação entre User e Donation
            modelBuilder.Entity<Donation>()
                .HasOne(d => d.Doador)
                .WithMany() // Um usuário pode fazer muitas doações, mas não temos uma lista de doações no modelo User
                .HasForeignKey(d => d.UsuarioId)
                .OnDelete(DeleteBehavior.Restrict); // Impede que um usuário seja deletado se ele tiver feito doações.
        }
    }
}