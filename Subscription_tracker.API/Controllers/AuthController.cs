using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Subscription_tracker.API.Data;
using Subscription_tracker.API.DTOs;
using Subscription_tracker.API.Models;
using Subscription_tracker.API.Services;

namespace Subscription_tracker.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext context, JwtTokenService tokenService) : ControllerBase
{
    private string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and password are required");

        if (dto.Password != dto.ConfirmPassword)
            return BadRequest("Passwords do not match");

        if (await context.Users.AnyAsync(u => u.Email == dto.Email))
            return BadRequest("Email already in use");

        var user = new User
        {
            Email = dto.Email,
            PasswordHash = HashPassword(dto.Password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var token = tokenService.GenerateToken(user.Id, user.Email);
        var response = new AuthResponseDto(token, new UserDto(user.Id, user.Email));

        return Ok(response);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Email) || string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest("Email and password are required");

        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null)
            return Unauthorized("Invalid email or password");

        var passwordHash = HashPassword(dto.Password);
        if (user.PasswordHash != passwordHash)
            return Unauthorized("Invalid email or password");

        var token = tokenService.GenerateToken(user.Id, user.Email);
        var response = new AuthResponseDto(token, new UserDto(user.Id, user.Email));

        return Ok(response);
    }
}
