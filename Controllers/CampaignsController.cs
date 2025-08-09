using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampanhaDoacaoAPI.Data;
using System.Security.Claims;
using ProjetoDoacao.Models;
using ProjetoDoacao.DTOs;

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

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetCampaigns()
        {
            return await _context.Campaigns
                     .Where(c => !c.IsDeleted)
                     .Include(c => c.Criador)
                     .ToListAsync();
        }

        [HttpGet("my-campaigns")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetMyCampaigns()
        {
            var campaigns = await _context.Campaigns
                                          .Where(c => !c.IsDeleted)
                                          .ToListAsync();

            return Ok(campaigns);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Campaign>> GetCampaign(int id)
        {
            var campaign = await _context.Campaigns
                .Where(c => !c.IsDeleted)
                .Include(c => c.Donations)
                .ThenInclude(d => d.Doador)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (campaign == null)
            {
                return NotFound();
            }
            return campaign;
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Campaign>> PostCampaign(Campaign campaign)
        {
            campaign.CriadorId = UserId;
            campaign.ValorArrecadado = 0;

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateCampaign(int id, [FromBody] CampaignUpdateDto campaignDto)
        {
            var campaign = await _context.Campaigns
                                         .FirstOrDefaultAsync(c => c.Id == id && c.CriadorId == UserId);

            if (campaign == null)
            {
                return NotFound("Campanha não encontrada ou você não tem permissão para editá-la.");
            }

            campaign.Titulo = campaignDto.Titulo;
            campaign.Descricao = campaignDto.Descricao;
            campaign.DataFim = campaignDto.DataFim;
            campaign.MetaArrecadacao = campaignDto.MetaArrecadacao;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var campaign = await _context.Campaigns
                                         .FirstOrDefaultAsync(c => c.Id == id && c.CriadorId == UserId);

            if (campaign == null)
            {
                return NotFound("Campanha não encontrada ou você não tem permissão para deletá-la.");
            }

            campaign.IsDeleted = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}