using System.Net.Mail;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Auth;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>User Authentication &amp; Account Management module (was routes/auth.js).</summary>
[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly TokenService _tokens;

    public AuthController(Db db, TokenService tokens) : base(db) => _tokens = tokens;

    // POST /api/auth/register
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest? body)
    {
        if (body == null) return Error(400, "Name is required.");
        var name = body.Name?.Trim();
        if (string.IsNullOrEmpty(name)) return Error(400, "Name is required.");
        if (!IsValidEmail(body.Email)) return Error(400, "A valid email is required.");
        if (body.Password == null || body.Password.Length < 6) return Error(400, "Password must be at least 6 characters.");
        if (body.Role == AppClaims.Owner)
            return Error(403, "Parking owner accounts are created by an administrator.");
        if (body.Role != null && body.Role != AppClaims.User) return Error(400, "Drivers can register here. Choose the driver account.");

        var email = body.Email!.Trim().ToLowerInvariant();
        await using var conn = await Db.OpenAsync();

        var exists = await conn.ExecuteScalarAsync<int?>("SELECT id FROM users WHERE email = @email", new { email });
        if (exists != null) return Error(409, "An account with this email already exists.");

        var hash = BCrypt.Net.BCrypt.HashPassword(body.Password, 10);
        var role = AppClaims.User;

        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO users (name, email, phone, password_hash, role) VALUES (@name, @email, @phone, @hash, @role);
              SELECT LAST_INSERT_ID();",
            new { name, email, phone = string.IsNullOrWhiteSpace(body.Phone) ? null : body.Phone.Trim(), hash, role });

        var user = await conn.QuerySingleAsync<User>("SELECT * FROM users WHERE id = @id", new { id });
        return StatusCode(201, new AuthResponse(_tokens.CreateToken(user), user));
    }

    // POST /api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest? body)
    {
        if (body == null || !IsValidEmail(body.Email) || string.IsNullOrEmpty(body.Password))
            return Error(400, "Email and password are required.");

        await using var conn = await Db.OpenAsync();
        var user = await conn.QuerySingleOrDefaultAsync<User>(
            "SELECT * FROM users WHERE email = @email", new { email = body.Email!.Trim().ToLowerInvariant() });

        if (user == null || !BCrypt.Net.BCrypt.Verify(body.Password, user.PasswordHash))
            return Error(401, "Invalid credentials");
        if (user.Status == "suspended")
            return Error(403, "This account has been suspended. Contact an administrator.");

        return Ok(new AuthResponse(_tokens.CreateToken(user), user));
    }

    // GET /api/auth/me
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        await using var conn = await Db.OpenAsync();
        var user = await conn.QuerySingleOrDefaultAsync<User>("SELECT * FROM users WHERE id = @id", new { id = CurrentUserId });
        if (user == null) return Error(404, "User not found.");
        return Ok(new { user });
    }

    private static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        try
        {
            var addr = new MailAddress(email.Trim());
            return addr.Address == email.Trim() && addr.Host.Contains('.');
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
