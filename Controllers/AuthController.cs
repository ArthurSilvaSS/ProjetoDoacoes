// Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using CampanhaDoacaoAPI.Data;
using CampanhaDoacaoAPI.Services;
using Microsoft.EntityFrameworkCore;
using ProjetoDoacao.Models;

namespace CampanhaDoacaoAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly TokenService _tokenService;

        public AuthController(AppDbContext context, TokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto userDto)
        {
            if (await _context.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return BadRequest("Email já cadastrado.");
            }

            var user = new User
            {
                Nome = userDto.Nome,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Senha)
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Usuário registrado com sucesso!" });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Senha, user.PasswordHash))
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { Token = token });
        }
    }
}