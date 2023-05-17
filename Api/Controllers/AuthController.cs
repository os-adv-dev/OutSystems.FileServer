﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Token([FromBody] string credentials)
    {
        // Decode the Base64 string
        var decodedCredentials = Encoding.UTF8.GetString(Convert.FromBase64String(credentials));

        // Split the username and password values
        var credentialsArray = decodedCredentials.Split(':');
        var username = credentialsArray[0];
        var password = credentialsArray[1];

        var isValid = ValidateCredentials(username, password);

        if (!isValid)
        {
            return Unauthorized("The credentials provided are not valid.");
        }

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new Claim[]
            {
                new Claim(ClaimTypes.Name, "username"),
                new Claim(ClaimTypes.Role, "admin")
            }),
            Expires = DateTime.UtcNow.AddMinutes(5),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return Ok(new
        {
            AccessToken = tokenHandler.WriteToken(token)
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