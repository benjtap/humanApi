using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Webhttp.Models;

namespace ConsoleAppToWebAPI
{
    public static class TokenUtils
    {
        public static string GenerateAccessToken(LoginRequest user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var configuration = new ConfigurationBuilder()
                     .AddJsonFile($"appsettings.json")
                ;
            var config = configuration.Build();

            var key = Encoding.ASCII.GetBytes(config.GetSection("Jwt:Key").Value);

            

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[] { new Claim("id", user.Email.ToString()) }),
              
                Expires = DateTime.UtcNow.AddHours(12), // AddHours(4) Token expiration timeyhhh55
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        public static string GenerateRefreshToken()
        {
            var randomNumber = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        public static string GenerateAccessTokenFromRefreshToken(string refreshToken )
        {
            // Implement logic to generate a new access token from the refresh token
            // Verify the refresh token and extract necessary information (e.g., user ID)
            // Then generate a new access token

            // For demonstration purposes, return a new token with an extended expiry
            var tokenHandler = new JwtSecurityTokenHandler();

            var configuration = new ConfigurationBuilder()
                     .AddJsonFile($"appsettings.json")
                ;
            var config = configuration.Build();

            var key = Encoding.ASCII.GetBytes(config.GetSection("Jwt:Key").Value);
            

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Expires = DateTime.UtcNow.AddHours(15), // Extend expiration time
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
