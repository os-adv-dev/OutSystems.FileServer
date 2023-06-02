using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OutSystems.FileServer.Api.Responses;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TokenResponse))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] LoginRequest credentials)
    {
        var isValid = ValidateCredentials(credentials.client_id, credentials.secret);

        if (!isValid)
        {
            return Unauthorized("The credentials provided are not valid.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var expiresIn = 5 * 1000;
        var expireDate = DateTime.UtcNow.AddMilliseconds(expiresIn);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "username"),
                new Claim(ClaimTypes.Role, "admin")
            }),
            Expires = expireDate,
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new TokenResponse
        {
            AccessToken = tokenHandler.WriteToken(token),
            ExpiresIn = expiresIn
        });
    }

    [Authorize]
    [HttpGet("validate-token")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult ValidateToken()
    {
        return Ok();
    }

    private bool ValidateCredentials(string username, string password)
    {
        if (username != _configuration["Credentials:UserName"] || password != _configuration["Credentials:Password"])
        {
            return false;
        }

        return true;
    }
}
