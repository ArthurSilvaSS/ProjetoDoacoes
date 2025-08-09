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

        [HttpPost("change-password")]
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

        [HttpPut("update-profile")]
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

        [HttpPost("delete-account")]
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