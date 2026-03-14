using Microsoft.IdentityModel.Tokens;
using Resonate_API.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Security.Claims;

namespace Resonate_API.Classes
{
    public class JwtToken
    {
        static byte[] Key = Encoding.UTF8.GetBytes("BLEBLEBLE_______BLUBLUBLU________PAMPAMBARAMBARAM");
        public static string Generate(Employees employee)
        {
            JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();
            SecurityTokenDescriptor tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim("EmployeeId", employee.Id.ToString()),
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Key), 
                    SecurityAlgorithms.HmacSha256Signature
                    )
            };
            SecurityToken Token = TokenHandler.CreateToken(tokenDescriptor);
            return TokenHandler.WriteToken(Token);
        }

        public static int? GetUserIdFromToken(string token)
        {
            try
            {
                JwtSecurityTokenHandler TokenHandler = new JwtSecurityTokenHandler();
                TokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken ValidatedToken);
                JwtSecurityToken JwtToken = (JwtSecurityToken)ValidatedToken;
                string EmployeeId = JwtToken.Claims.First(x => x.Type == "EmployeeId").Value;
                return int.Parse(EmployeeId);
            }
            catch
            {
                return null;
            }
        }
    }
}
