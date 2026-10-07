using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AnuradhapuraAI.Application.Authentication;
using AnuradhapuraAI.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AnuradhapuraAI.Infrastructure.Authentication;

public sealed class JwtTokenService(
    IOptions<JwtSettings> jwtSettings,
    TimeProvider timeProvider) : ITokenService
{
    public TokenResult CreateToken(User user, string roleName)
    {
        var settings = jwtSettings.Value;
        var now = timeProvider.GetUtcNow();
        var expiresAt = now.AddMinutes(settings.ExpirationMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, roleName)
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SigningKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new TokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }
}
