using CampanhaDoacaoAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjetoDoacao.DTOs;
using System.Security.Claims;

namespace ProjetoDoacao.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Altera a senha do usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Requer a senha atual para confirmação da alteração.
        /// </remarks>
        /// <param name="dto">Objeto contendo a senha atual e a nova senha.</param>
        /// <response code="200">Senha alterada com sucesso.</response>
        /// <response code="400">Se a senha atual estiver incorreta.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpPost("change-password")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Senha atual incorreta.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Senha alterada com sucesso." });
        }

        /// <summary>
        /// Atualiza o email do usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Requer a senha atual para confirmação da alteração.
        /// </remarks>
        /// <param name="dto">Objeto contendo o novo email e a senha atual.</param>
        /// <response code="200">Perfil atualizado com sucesso.</response>
        /// <response code="400">Se a senha atual estiver incorreta ou o novo email já estiver em uso.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpPut("update-profile")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
        {
            var user = await _context.Users.FindAsync(UserId);
            if (user == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
            {
                return BadRequest("Senha atual incorreta.");
            }

            if (await _context.Users.AnyAsync(u => u.Email == dto.NewEmail && u.Id != UserId))
            {
                return BadRequest("Este email já está em uso por outra conta.");
            }

            user.Email = dto.NewEmail;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Perfil atualizado com sucesso." });
        }

        /// <summary>
        /// Desativa (soft delete) a conta do usuário autenticado.
        /// </summary>
        /// <remarks>
        /// Ação crítica que requer a senha do usuário para confirmação. Desativa o usuário e todas as suas campanhas associadas.
        /// </remarks>
        /// <param name="dto">Objeto contendo a senha para confirmação.</param>
        /// <response code="200">Conta desativada com sucesso.</response>
        /// <response code="400">Se a senha de confirmação estiver incorreta.</response>
        /// <response code="401">Se o usuário não estiver autenticado.</response>
        [HttpPost("delete-account")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == UserId && !u.IsDeleted);

            if (user == null)
            {
                return NotFound("Usuário não encontrado.");
            }

            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            {
                return BadRequest("Senha incorreta. A conta não foi deletada.");
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

            return Ok(new { Message = "Sua conta e todas as suas campanhas foram desativadas com sucesso." });
        }
    }
}