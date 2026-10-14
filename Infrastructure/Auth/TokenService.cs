using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Auth
{
    public class TokenService
    {
        private readonly IConfiguration configuration;
        public TokenService(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public string Create(UserAccount user, UserSession session, DateTime expires)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("sid", session.Id.ToString()),
                new Claim("security_stamp", user.SecurityStamp.ToString()),
                new Claim("token_use", "access"),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };
            if (user.OrganizationId is int organizationId)
            {
                claims.Add(new Claim("organization_id", organizationId.ToString()));
            }

            var key = configuration["JWT:Key"] ?? throw new InvalidOperationException("JWT:Key is not configured.");
            var token = new JwtSecurityToken(configuration["JWT:Issuer"] ?? "ChatbotApi", configuration["JWT:Audience"] ?? "ChatbotConsole", claims, expires: expires, signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
