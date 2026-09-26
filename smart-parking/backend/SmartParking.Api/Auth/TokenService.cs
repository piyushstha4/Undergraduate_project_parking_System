using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using SmartParking.Api.Models;

namespace SmartParking.Api.Auth;

/// <summary>Issues JWTs with the same payload as the Node version: { id, role, name, email }.</summary>
public class TokenService
{
    private readonly byte[] _key;
    private readonly int _expiresInDays;

    public TokenService(IConfiguration config)
    {
        _key = Encoding.UTF8.GetBytes(JwtSetup.GetSecret(config));
        _expiresInDays = config.GetValue("Jwt:ExpiresInDays", 7);
    }

    public string CreateToken(User user)
    {
        var claims = new[]
        {
            new Claim(AppClaims.Id, user.Id.ToString(), ClaimValueTypes.Integer32),
            new Claim(AppClaims.Role, user.Role),
            new Claim(AppClaims.Name, user.Name),
            new Claim(AppClaims.Email, user.Email),
        };
        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(_expiresInDays),
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(_key), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
