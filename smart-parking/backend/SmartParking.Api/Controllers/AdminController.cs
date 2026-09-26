using System.Net.Mail;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Auth;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>Admin Dashboard &amp; Monitoring module (was routes/admin.js).</summary>
[Authorize(Roles = "admin")]
[Route("api/admin")]
public class AdminController : ApiControllerBase
{
    public AdminController(Db db) : base(db) { }

    // GET /api/admin/stats — system-wide dashboard overview
    [HttpGet("stats")]
    public async Task<IActionResult> Stats()
    {
        await using var conn = await Db.OpenAsync();
        var stats = await conn.QuerySingleAsync<AdminStatsResponse>(@"
            SELECT
              (SELECT COUNT(*) FROM users WHERE role = 'user')          AS TotalUsers,
              (SELECT COUNT(*) FROM users WHERE role = 'parking_owner') AS TotalOwners,
              (SELECT COUNT(*) FROM parking_areas)                      AS TotalAreas,
              (SELECT COUNT(*) FROM slots)                              AS TotalSlots,
              (SELECT COUNT(*) FROM slots WHERE status = 'available')   AS AvailableSlots,
              (SELECT COUNT(*) FROM bookings WHERE status = 'confirmed') AS ActiveBookings,
              (SELECT COUNT(*) FROM bookings)                           AS TotalBookings,
              (SELECT COALESCE(SUM(amount), 0) FROM payments WHERE status = 'success') AS Revenue");

        stats.BookingsByStatus = (await conn.QueryAsync<StatusCount>(
            "SELECT status, COUNT(*) AS count FROM bookings GROUP BY status ORDER BY status")).ToList();

        stats.RecentBookings = (await conn.QueryAsync<RecentBooking>(@"
            SELECT b.id, b.status, b.total_amount, b.created_at, u.name AS user_name, pa.name AS area_name
            FROM bookings b
            JOIN users u ON u.id = b.user_id
            JOIN slots s ON s.id = b.slot_id
            JOIN parking_areas pa ON pa.id = s.parking_area_id
            ORDER BY b.created_at DESC, b.id DESC LIMIT 8")).ToList();

        return Ok(stats);
    }

    // GET /api/admin/users?role=user|parking_owner
    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] string? role)
    {
        var sql = "SELECT id, name, email, phone, role, status, created_at FROM users WHERE role <> 'admin'";
        if (!string.IsNullOrEmpty(role)) sql += " AND role = @role";
        sql += " ORDER BY created_at DESC, id DESC";

        await using var conn = await Db.OpenAsync();
        var users = await conn.QueryAsync<User>(sql, new { role });
        return Ok(new { users });
    }

    // POST /api/admin/users — admin creates a parking owner and chooses their login
    [HttpPost("users")]
    public async Task<IActionResult> CreateOwner([FromBody] RegisterRequest? body)
    {
        if (body == null || string.IsNullOrWhiteSpace(body.Name)) return Error(400, "Name is required.");
        if (!IsValidEmail(body.Email)) return Error(400, "A valid email is required.");
        if (body.Password == null || body.Password.Length < 6) return Error(400, "Password must be at least 6 characters.");
        if (body.Role != null && body.Role != AppClaims.Owner)
            return Error(400, "An administrator can create parking owner accounts. Drivers register themselves.");

        var email = body.Email!.Trim().ToLowerInvariant();
        await using var conn = await Db.OpenAsync();
        var exists = await conn.ExecuteScalarAsync<int?>("SELECT id FROM users WHERE email = @email", new { email });
        if (exists != null) return Error(409, "An account with this email already exists.");

        var hash = BCrypt.Net.BCrypt.HashPassword(body.Password, 10);
        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO users (name, email, phone, password_hash, role)
              VALUES (@name, @email, @phone, @hash, 'parking_owner');
              SELECT LAST_INSERT_ID();",
            new
            {
                name = body.Name.Trim(),
                email,
                phone = string.IsNullOrWhiteSpace(body.Phone) ? null : body.Phone.Trim(),
                hash,
            });

        var user = await conn.QuerySingleAsync<User>(
            "SELECT id, name, email, phone, role, status, created_at FROM users WHERE id = @id", new { id });
        return StatusCode(201, new { user });
    }

    // PUT /api/admin/users/{id}/password — admin sets the password they will give to a parking owner
    [HttpPut("users/{id:int}/password")]
    public async Task<IActionResult> SetPassword(int id, [FromBody] SetPasswordRequest? body)
    {
        if (body?.Password == null || body.Password.Length < 6)
            return Error(400, "Password must be at least 6 characters.");

        await using var conn = await Db.OpenAsync();
        var user = await conn.QuerySingleOrDefaultAsync<User>("SELECT * FROM users WHERE id = @id", new { id });
        if (user == null) return Error(404, "User not found.");
        if (user.Role != AppClaims.Owner)
            return Error(403, "Only a parking owner's password is set by an administrator.");

        var hash = BCrypt.Net.BCrypt.HashPassword(body.Password, 10);
        await conn.ExecuteAsync("UPDATE users SET password_hash = @hash WHERE id = @id", new { hash, id });
        return Ok(new { success = true });
    }

    // PUT /api/admin/users/{id}/status — suspend / reactivate a user or owner
    [HttpPut("users/{id:int}/status")]
    public async Task<IActionResult> SetStatus(int id, [FromBody] UserStatusRequest? body)
    {
        var status = body?.Status;
        if (status is not ("active" or "suspended")) return Error(400, "Status must be \"active\" or \"suspended\".");

        await using var conn = await Db.OpenAsync();
        var user = await conn.QuerySingleOrDefaultAsync<User>("SELECT * FROM users WHERE id = @id", new { id });
        if (user == null) return Error(404, "User not found.");
        if (user.Role == "admin") return Error(403, "Cannot modify an admin account.");

        await conn.ExecuteAsync("UPDATE users SET status = @status WHERE id = @id", new { status, id });
        var updated = await conn.QuerySingleAsync<User>(
            "SELECT id, name, email, phone, role, status, created_at FROM users WHERE id = @id", new { id });
        return Ok(new { user = updated });
    }

    // GET /api/admin/parking-areas — every facility with owner info
    [HttpGet("parking-areas")]
    public async Task<IActionResult> ParkingAreas()
    {
        await using var conn = await Db.OpenAsync();
        var areas = await conn.QueryAsync<AdminParkingArea>(@"
            SELECT pa.*, u.name AS owner_name, u.email AS owner_email,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots
            FROM parking_areas pa JOIN users u ON u.id = pa.owner_id
            ORDER BY pa.created_at DESC, pa.id DESC");
        return Ok(new { areas });
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
