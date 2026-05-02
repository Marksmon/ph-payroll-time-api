using System.Security.Cryptography;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace PhPayrollTimeApi.Api.Services;

public class RsaTestTokenService : ITestTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly RSA _rsa;

    public RsaTestTokenService(IConfiguration configuration)
    {
        _issuer = configuration["Jwt:Issuer"]!;
        _audience = configuration["Jwt:Audience"]!;
        var pem = File.ReadAllText(configuration["Jwt:PrivateKeyPath"]!);
        _rsa = RSA.Create();
        _rsa.ImportFromPem(pem);
    }

    public string GenerateToken(string sub, string role, int expiryHours = 1)
    {
        var handler = new JsonWebTokenHandler();
        var descriptor = new SecurityTokenDescriptor
        {
            Claims = new Dictionary<string, object>
            {
                { "sub", sub },
                { "role", role }
            },
            Issuer = _issuer,
            Audience = _audience,
            Expires = DateTime.UtcNow.AddHours(expiryHours),
            IssuedAt = DateTime.UtcNow,
            SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsa), SecurityAlgorithms.RsaSha256)
        };
        return handler.CreateToken(descriptor);
    }
}
