using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Data;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly PasswordHasher<UserAccount> _passwordHasher = new();

    public AuthController(IUserRepository userRepository, IJwtTokenService jwtTokenService)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email y contraseña son obligatorios." });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "La contraseña debe tener al menos 6 caracteres." });
        }

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return Conflict(new { message = "Ya existe una cuenta con ese email." });
        }

        var toCreate = new UserAccount { Email = request.Email.Trim().ToLowerInvariant() };
        var hash = _passwordHasher.HashPassword(toCreate, request.Password);
        var created = await _userRepository.CreateAsync(toCreate.Email, hash);

        var token = _jwtTokenService.GenerateToken(created);
        return Ok(token);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Email y contraseña son obligatorios." });
        }

        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        PasswordVerificationResult verification;
        try
        {
            verification = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        }
        catch (FormatException)
        {
            // Legacy or malformed hashes should not break login flow with 500 errors.
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        if (verification == PasswordVerificationResult.Failed)
        {
            return Unauthorized(new { message = "Credenciales inválidas." });
        }

        var token = _jwtTokenService.GenerateToken(user);
        return Ok(token);
    }
}