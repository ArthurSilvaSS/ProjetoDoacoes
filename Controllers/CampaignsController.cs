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

        /// <summary>
        /// Lista todas as campanhas ativas.
        /// </summary>
        /// <remarks>
        /// Este endpoint é público e retorna uma lista de todas as campanhas que não foram marcadas como excluídas.
        /// </remarks>
        /// <returns>Uma lista de campanhas.</returns>
        /// <response code="200">Retorna a lista de campanhas.</response>

        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Campaign>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetCampaigns()
        {
            return await _context.Campaigns
                     .Where(c => !c.IsDeleted)
                     .Include(c => c.Criador)
                     .ToListAsync();
        }


        /// <summary>
        /// Lista as campanhas criadas pelo usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Requer autenticação. Retorna uma lista de todas as campanhas ativas criadas pelo usuário do token JWT.
        /// </remarks>
        /// <returns>Uma lista de campanhas do usuário.</returns>
        /// <response code="200">Retorna a lista de campanhas do usuário.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpGet("my-campaigns")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Campaign>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetMyCampaigns()
        {
            var campaigns = await _context.Campaigns
                                          .Where(c => c.CriadorId == UserId && !c.IsDeleted)
                                          .ToListAsync();

            return Ok(campaigns);
        }

        /// <summary>
        /// Busca uma campanha específica pelo seu ID.
        /// </summary>
        /// <remarks>
        /// Retorna os detalhes completos de uma campanha, incluindo as doações associadas.
        /// Este endpoint é público e não requer autenticação.
        /// </remarks>
        /// <param name="id">O ID numérico da campanha a ser buscada.</param>
        /// <returns>O objeto da campanha encontrada.</returns>
        /// <response code="200">Retorna a campanha solicitada com sucesso.</response>
        /// <response code="404">Se a campanha com o ID especificado não for encontrada.</response>
   
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Campaign), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Cria uma nova campanha.
        /// </summary>
        /// <remarks>
        /// Requer autenticação. O ID do criador é automaticamente associado com base no token JWT do usuário.
        /// </remarks>
        /// <param name="campaign">O objeto da campanha a ser criada.</param>
        /// <returns>A campanha recém-criada.</returns>
        /// <response code="201">Retorna a campanha recém-criada com a URL de acesso.</response>
        /// <response code="400">Se os dados da campanha forem inválidos.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Campaign), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Campaign>> PostCampaign(Campaign campaign)
        {
            campaign.CriadorId = UserId;
            campaign.ValorArrecadado = 0;

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }

        /// <summary>
        /// Atualiza uma campanha existente do usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Requer autenticação. O usuário deve ser o criador da campanha para poder atualizá-la.
        /// </remarks>
        /// <param name="id">O ID da campanha a ser atualizada.</param>
        /// <param name="campaignDto">Os dados da campanha a serem atualizados.</param>
        /// <response code="204">Campanha atualizada com sucesso.</response>
        /// <response code="400">Se os dados de atualização forem inválidos.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="404">Se a campanha não for encontrada ou não pertencer ao usuário.</response>
        
        [HttpPut("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        /// <summary>
        /// Desativa (soft delete) uma campanha do usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Requer autenticação. O usuário deve ser o criador da campanha. A campanha não é apagada fisicamente, apenas marcada como inativa.
        /// </remarks>
        /// <param name="id">O ID da campanha a ser desativada.</param>
        /// <response code="204">Campanha desativada com sucesso.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="404">Se a campanha não for encontrada ou não pertencer ao usuário.</response>
        
        [HttpDelete("{id}")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
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