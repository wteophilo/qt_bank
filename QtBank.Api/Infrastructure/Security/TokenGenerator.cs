using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace QtBank.Api.Infrastructure.Security;

public static class TokenGenerator
{
    // A secure key of 256 bits (32 bytes) for HMAC-SHA256
    public const string Secret = "SuperSecretKeyThatIsAtLeast32BytesLong!";
    public const string Issuer = "QtBankApi";
    public const string Audience = "QtBankApiUsers";

    public static string GenerateToken(string username, string role = "User")
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(Secret);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim("SessionId", Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            Issuer = Issuer,
            Audience = Audience,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
