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

        /// <summary>
        /// Registra um novo usuário comum no sistema.
        /// </summary>
        /// <remarks>
        /// Endpoint público para criação de novas contas. Por padrão, todos os novos usuários são criados com o papel 'Comum'.
        /// </remarks>
        /// <param name="userDto">Dados necessários para o registro: Nome, Email e Senha.</param>
        /// <response code="200">Usuário registrado com sucesso.</response>
        /// <response code="400">Se o email já estiver cadastrado ou os dados forem inválidos.</response>
        [HttpPost("register")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Autentica um usuário e retorna um token JWT.
        /// </summary>
        /// <remarks>
        /// Endpoint público. Envie email e senha para receber um token de autenticação Bearer para usar em rotas protegidas.
        /// </remarks>
        /// <param name="loginDto">Credenciais de login (Email e Senha).</param>
        /// <returns>Um objeto contendo o token JWT.</returns>
        /// <response code="200">Login bem-sucedido, retorna o token.</response>
        /// <response code="401">Credenciais inválidas ou conta desativada.</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] UserLoginDto loginDto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == loginDto.Email && !u.IsDeleted);

            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Senha, user.PasswordHash))
            {
                return Unauthorized("Email ou senha inválidos.");
            }

            var token = _tokenService.GenerateToken(user);
            return Ok(new { Token = token });
        }
    }
}