using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CurriculumGenerator.Data;
using CurriculumGenerator.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CurriculumGenerator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<IdentityUser> userManager,
        ApplicationDbContext db,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _db = db;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "Todos os campos são obrigatórios." });

        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(new { message = "Este e-mail já está cadastrado." });

        var user = new IdentityUser
        {
            UserName = request.Email,
            Email = request.Email
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(new { message = result.Errors.First().Description });

        await _userManager.AddClaimAsync(user, new Claim("name", request.Name));

        var plan = "free";
        var token = GenerateToken(user, request.Name, plan);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Name = request.Name,
                Email = user.Email!,
                Plan = plan
            }
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest(new { message = "E-mail e senha são obrigatórios." });

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return BadRequest(new { message = "E-mail ou senha inválidos." });

        if (!await _userManager.CheckPasswordAsync(user, request.Password))
            return BadRequest(new { message = "E-mail ou senha inválidos." });

        var claims = await _userManager.GetClaimsAsync(user);
        var name = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? user.UserName ?? "";

        var plan = await _db.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == "active")
            .Select(s => s.Plan)
            .FirstOrDefaultAsync() ?? "free";

        var token = GenerateToken(user, name, plan);

        return Ok(new AuthResponse
        {
            Token = token,
            User = new UserInfo
            {
                Id = user.Id,
                Name = name,
                Email = user.Email!,
                Plan = plan
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return Unauthorized();

        var claims = await _userManager.GetClaimsAsync(user);
        var name = claims.FirstOrDefault(c => c.Type == "name")?.Value ?? user.UserName ?? "";

        var plan = await _db.Subscriptions
            .Where(s => s.UserId == user.Id && s.Status == "active")
            .Select(s => s.Plan)
            .FirstOrDefaultAsync() ?? "free";

        return Ok(new UserInfo
        {
            Id = user.Id,
            Name = name,
            Email = user.Email!,
            Plan = plan
        });
    }

    private string GenerateToken(IdentityUser user, string name, string plan)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]!));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, name),
            new Claim(ClaimTypes.Email, user.Email!),
            new Claim("plan", plan)
        };

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
