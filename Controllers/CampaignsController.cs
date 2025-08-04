// Controllers/CampaignsController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampanhaDoacaoAPI.Data;
using System.Security.Claims;
using ProjetoDoacao.Models;

namespace CampanhaDoacaoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CampaignsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CampaignsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/campaigns - Listar todas as campanhas (público)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetCampaigns()
        {
            return await _context.Campaigns.Include(c => c.Criador).ToListAsync();
        }

        // GET: api/campaigns/{id} - Detalhes de uma campanha (público)
        [HttpGet("{id}")]
        public async Task<ActionResult<Campaign>> GetCampaign(int id)
        {
            var campaign = await _context.Campaigns
                .Include(c => c.Donations)
                .ThenInclude(d => d.Doador)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
            {
                return NotFound();
            }
            return campaign;
        }

        // POST: api/campaigns - Criar uma nova campanha (protegido)
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Campaign>> PostCampaign(Campaign campaign)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized();
            }

            campaign.CriadorId = int.Parse(userId);
            campaign.ValorArrecadado = 0; // Inicia com zero

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }
    }
}