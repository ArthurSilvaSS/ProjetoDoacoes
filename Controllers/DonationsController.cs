// Controllers/DonationsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using CampanhaDoacaoAPI.Data;
using System.Security.Claims;
using ProjetoDoacao.Models;

namespace CampanhaDoacaoAPI.Controllers
{
    [ApiController]
    [Route("api/campaigns/{campaignId}/donations")] // Rota aninhada
    [Authorize] // Apenas usuários logados podem doar
    public class DonationsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DonationsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public async Task<IActionResult> CreateDonation(int campaignId, [FromBody] Donation donation)
        {
            var campaign = await _context.Campaigns.FindAsync(campaignId);
            if (campaign == null)
            {
                return NotFound("Campanha não encontrada.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            donation.CampanhaId = campaignId;
            donation.UsuarioId = int.Parse(userId);
            donation.DataDoacao = DateTime.UtcNow;

            // Atualiza o valor arrecadado na campanha
            campaign.ValorArrecadado += donation.Valor;

            _context.Donations.Add(donation);
            await _context.SaveChangesAsync();

            return Ok(donation);
        }
    }
}