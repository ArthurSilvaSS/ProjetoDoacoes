using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ProjetoDoacao.Models;
using ProjetoDoacao.DTOs;
using ProjetoDoacao.Helpers;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using CampanhaDoacaoAPI.Data;

namespace ProjetoDoacao.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class CampaignsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CampaignsController(AppDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);


        /// <summary>
        /// Busca uma lista paginada de todas as campanhas públicas e ativas.
        /// </summary>
        /// <remarks>Permite a paginação e a busca por termo no título. Endpoint público.</remarks>
        /// <param name="pageNumber">O número da página a ser retornada (padrão: 1).</param>
        /// <param name="pageSize">A quantidade de campanhas por página (padrão: 6).</param>
        /// <param name="search">Termo opcional para buscar no título das campanhas.</param>
        /// <returns>Um objeto com a lista de campanhas da página e o número total de campanhas.</returns>
        /// <response code="200">Retorna a lista paginada de campanhas.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Campaign>), StatusCodes.Status200OK)]
        public async Task<ActionResult<PagedResult<Campaign>>> GetCampaigns(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 6,
            [FromQuery] string? search = null)
        {
            var query = _context.Campaigns
                                .Where(c => !c.IsDeleted)
                                .Include(c => c.Criador)
                                .OrderByDescending(c => c.DataInicio)
                                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(c => c.Titulo.Contains(search));
            }

            var totalCount = await query.CountAsync();
            var campaigns = await query
                                    .Skip((pageNumber - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToListAsync();

            return Ok(new PagedResult<Campaign> { Items = campaigns, TotalCount = totalCount });
        }

        /// <summary>
        /// Busca uma campanha específica e ativa pelo seu ID.
        /// </summary>
        /// <remarks>Retorna os detalhes completos, incluindo doações e doadores. Endpoint público.</remarks>
        /// <param name="id">O ID da campanha.</param>
        /// <returns>O objeto da campanha encontrada.</returns>
        /// <response code="200">Retorna a campanha encontrada.</response>
        /// <response code="404">Se a campanha não for encontrada ou estiver inativa.</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Campaign), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<Campaign>> GetCampaign(int id)
        {
            var campaign = await _context.Campaigns
                .Where(c => !c.IsDeleted && c.Id == id)
                .Include(c => c.Donations)
                .ThenInclude(d => d.Doador)
                .FirstOrDefaultAsync();

            if (campaign == null)
            {
                return NotFound();
            }
            return Ok(campaign);
        }

        // --- ENDPOINTS AUTENTICADOS ---

        /// <summary>
        /// Lista todas as campanhas ativas criadas pelo usuário autenticado.
        /// </summary>
        /// <remarks>Requer autenticação. Retorna uma lista simples (não paginada) das campanhas do próprio usuário.</remarks>
        /// <returns>Uma lista das campanhas do usuário.</returns>
        /// <response code="200">Retorna a lista de campanhas.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpGet("my-campaigns")]
        [Authorize]
        [ProducesResponseType(typeof(IEnumerable<Campaign>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetMyCampaigns()
        {
            var campaigns = await _context.Campaigns
                                          .Where(c => c.CriadorId == UserId && !c.IsDeleted)
                                          .OrderByDescending(c => c.DataInicio)
                                          .ToListAsync();

            return Ok(campaigns);
        }

        /// <summary>
        /// Cria uma nova campanha com upload de imagem opcional.
        /// </summary>
        /// <remarks>Requer autenticação. Os dados devem ser enviados como `multipart/form-data`.</remarks>
        /// <param name="campaignDto">DTO com os dados da campanha e o ficheiro da imagem.</param>
        /// <returns>A campanha recém-criada.</returns>
        /// <response code="201">Retorna a campanha criada e a sua localização.</response>
        /// <response code="400">Se os dados fornecidos forem inválidos.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(Campaign), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<Campaign>> PostCampaign([FromForm] CampaignCreateDto campaignDto)
        {
            var campaign = new Campaign
            {
                Titulo = campaignDto.Titulo,
                Descricao = campaignDto.Descricao,
                MetaArrecadacao = campaignDto.MetaArrecadacao,
                DataInicio = campaignDto.DataInicio,
                DataFim = campaignDto.DataFim,
                CriadorId = UserId,
            };

            if (campaignDto.ImagemArquivo != null && campaignDto.ImagemArquivo.Length > 0)
            {
                var nomeArquivoUnico = Guid.NewGuid().ToString() + Path.GetExtension(campaignDto.ImagemArquivo.FileName);
                var caminhoUploads = Path.Combine(_environment.WebRootPath, "uploads");
                var caminhoArquivo = Path.Combine(caminhoUploads, nomeArquivoUnico);

                Directory.CreateDirectory(caminhoUploads);

                using (var fileStream = new FileStream(caminhoArquivo, FileMode.Create))
                {
                    await campaignDto.ImagemArquivo.CopyToAsync(fileStream);
                }

                var request = HttpContext.Request;
                var urlBase = $"{request.Scheme}://{request.Host}";
                campaign.ImagemUrl = $"{urlBase}/uploads/{nomeArquivoUnico}";
            }

            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetCampaign), new { id = campaign.Id }, campaign);
        }

        /// <summary>
        /// Atualiza os dados de uma campanha existente do usuário autenticado.
        /// </summary>
        /// <remarks>Requer autenticação. O usuário deve ser o criador da campanha.</remarks>
        /// <param name="id">O ID da campanha a ser atualizada.</param>
        /// <param name="campaignDto">Os novos dados para a campanha.</param>
        /// <response code="204">Indica que a campanha foi atualizada com sucesso.</response>
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
                                         .FirstOrDefaultAsync(c => c.Id == id && c.CriadorId == UserId && !c.IsDeleted);

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
        /// <remarks>Requer autenticação. O usuário deve ser o criador. A campanha é marcada como inativa.</remarks>
        /// <param name="id">O ID da campanha a ser desativada.</param>
        /// <response code="204">Indica que a campanha foi desativada com sucesso.</response>
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
                                         .FirstOrDefaultAsync(c => c.Id == id && c.CriadorId == UserId && !c.IsDeleted);

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