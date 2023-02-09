using GrpcServiceDemo.Protos;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace GrpcServiceDemo.AuthenticationUtils
{
    public class JwtAuthenticationManager
    {
        public const string JWT_TOKEN_KEY = "LiquidAtUniteCopenhagen2019WithRacctello";
        private const int JWT_TOKEN_VALIDITY = 30;
        public static AuthenticationResponse Authenticate(AuthenticationRequest authenticationRequest)
        {
            // --- Implement User Credentials Validation
            string UserRole=string.Empty;
            if (authenticationRequest.UserName == "admin" && authenticationRequest.Password == "admin")
                UserRole = "Administrator";
            else if (authenticationRequest.UserName == "user" && authenticationRequest.Password == "user")
                UserRole = "User";
            else
                return null;

            var jwtSecurityHandler = new JwtSecurityTokenHandler();
            var tokenKey = Encoding.ASCII.GetBytes(JWT_TOKEN_KEY);
            var tokenExpiryDateTime=DateTime.Now.AddMinutes(JWT_TOKEN_VALIDITY);
            var securityTokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new System.Security.Claims.ClaimsIdentity(new List<System.Security.Claims.Claim>
                {
                    new System.Security.Claims.Claim("username", authenticationRequest.UserName),
                    new System.Security.Claims.Claim(ClaimTypes.Role, UserRole)
                }),
                Expires = tokenExpiryDateTime,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tokenKey),
                SecurityAlgorithms.HmacSha256Signature)

            };
            var securityToken = jwtSecurityHandler.CreateToken(securityTokenDescriptor);
            var token=jwtSecurityHandler.WriteToken(securityToken);
            return new AuthenticationResponse
            {
                AccessToken = token,
                ExpiresIn = (int)tokenExpiryDateTime.Subtract(DateTime.Now).TotalSeconds
            };
        }
    }
}
