using System.ComponentModel.DataAnnotations;

public record UserRegisterDto([Required] string Nome, [Required][EmailAddress] string Email, [Required] string Senha);