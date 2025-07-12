using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace bosphorus_fellas_api.Services;

public class JwtService
{
    private readonly string _secretKey;
    private readonly int _expiryTimeInSeconds;

    public JwtService(IConfiguration configuration)
    {
        _secretKey = configuration["Jwt:SecretKey"] ?? throw new ArgumentNullException("JWT SecretKey bulunamadı");
        _expiryTimeInSeconds = int.Parse(configuration["Jwt:ExpiryTimeInSeconds"] ?? "3600");
    }

    public string GenerateToken(int userId, string userType, string email, string ad, string soyad)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_secretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Name, $"{ad} {soyad}"),
            new Claim("UserType", userType),
            new Claim("Ad", ad),
            new Claim("Soyad", soyad)
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddSeconds(_expiryTimeInSeconds),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            Issuer = "BosphorusFellasAPI",
            Audience = "BosphorusFellasAPI"
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = "BosphorusFellasAPI",
                ValidateAudience = true,
                ValidAudience = "BosphorusFellasAPI",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out SecurityToken validatedToken);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetExpiryTime()
    {
        return DateTime.UtcNow.AddSeconds(_expiryTimeInSeconds);
    }
} 