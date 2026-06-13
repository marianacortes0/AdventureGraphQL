using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace AdventureGraphQL.Api.Auth;

/// <summary>
/// Genera (firma) los JSON Web Tokens del login.
/// La clave y los datos del emisor salen de la sección "Jwt" de la configuración.
/// </summary>
public class TokenService
{
    private readonly IConfiguration _config;

    public TokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime ExpiresAt) Create(string username)
    {
        var jwt = _config.GetSection("Jwt");

        // Clave simétrica: el MISMO secreto firma y luego valida el token.
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expires = DateTime.UtcNow.AddMinutes(
            int.Parse(jwt["ExpiresMinutes"] ?? "60"));

        // "Claims" = datos que viajan dentro del token (quién es y un id único).
        // El claim de rol habilita [Authorize(Roles = "Gestor")] en las mutations
        // (Escenario D). Usamos ClaimTypes.Role para que la validación JWT lo
        // reconozca como rol sin configuración extra.
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, "Gestor")
        };

        var token = new JwtSecurityToken(
            issuer: jwt["Issuer"],
            audience: jwt["Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
