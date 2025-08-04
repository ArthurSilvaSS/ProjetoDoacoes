using System.ComponentModel.DataAnnotations;

public record UserLoginDto([Required][EmailAddress] string Email, [Required] string Senha);