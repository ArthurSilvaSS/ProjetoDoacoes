using CampanhaDoacaoAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoDoacao.Models;

namespace ProjetoDoacao.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    [Produces("application/json")]
    public class AdminController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Lista todos os usuários cadastrados no sistema.
        /// </summary>
        /// <remarks>
        /// Acesso restrito a administradores. Retorna todos os usuários, incluindo os desativados.
        /// </remarks>
        /// <returns>Uma lista de todos os usuários.</returns>
        /// <response code="200">Retorna a lista de usuários.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não for um administrador.</response>
        [HttpGet("users")]
        [ProducesResponseType(typeof(IEnumerable<User>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        /// <summary>
        /// Desativa (soft delete) um usuário específico pelo ID.
        /// </summary>
        /// <remarks>
        /// Acesso restrito a administradores. Esta ação também desativa em cascata todas as campanhas do usuário.
        /// </remarks>
        /// <param name="id">O ID do usuário a ser desativado.</param>
        /// <response code="204">Usuário desativado com sucesso.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        /// <response code="403">Se o usuário autenticado não for um administrador.</response>
        /// <response code="404">Se o usuário com o ID especificado não for encontrado.</response>
        [HttpDelete("users/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound("Usuário não encontrado.")
                    ;
            }
            user.IsDeleted = true;
            var userCampaigns = await _context.Campaigns
                                      .Where(c => c.CriadorId == user.Id && !c.IsDeleted)
                                      .ToListAsync();

            foreach (var campaign in userCampaigns)
            {
                campaign.IsDeleted = true;
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Desativa (soft delete) qualquer campanha pelo ID.
        /// </summary>
        /// <remarks>Acesso restrito a administradores.</remarks>
        /// <param name="id">O ID da campanha a ser desativada.</param>
        /// <response code="204">Campanha desativada com sucesso.</response>
        /// <response code="401">Não autenticado.</response>
        /// <response code="403">Não autorizado (não é Admin).</response>
        /// <response code="404">Se a campanha não for encontrada.</response>
        [HttpDelete("campaigns/{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteCampaign(int id)
        {
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign == null)
            {
                return NotFound("Campanha não encontrada.");
            }

            campaign.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Campanha apagada com sucesso pelo administrador." });
        }

        /// <summary>
        /// (Admin) Lista TODAS as campanhas do sistema.
        /// </summary>
        /// <remarks>Acesso restrito a administradores. Retorna todas as campanhas, incluindo as inativas.</remarks>
        [HttpGet("campaigns")]
        [ProducesResponseType(typeof(IEnumerable<Campaign>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<Campaign>>> GetAllCampaigns()
        {
            return await _context.Campaigns.Include(c => c.Criador).ToListAsync();
        }
    }
}