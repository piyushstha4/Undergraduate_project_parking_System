using System.Globalization;
using System.Security.Cryptography;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;
using SmartParking.Api.Services;

namespace SmartParking.Api.Controllers;

/// <summary>Slot Booking &amp; Reservation module + simulated Payment module (was routes/bookings.js).</summary>
[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    private static readonly string[] PaymentMethods = { "esewa", "khalti", "card" };

    private const string BookingDetailSql = @"
        SELECT b.*, s.slot_number, s.vehicle_type, pa.name AS area_name, pa.address AS area_address,
               pa.city AS area_city, pa.id AS area_id, u.name AS user_name, u.email AS user_email
        FROM bookings b
        JOIN slots s ON s.id = b.slot_id
        JOIN parking_areas pa ON pa.id = s.parking_area_id
        JOIN users u ON u.id = b.user_id";

    private readonly BookingReleaseService _release;

    public BookingsController(Db db, BookingReleaseService release) : base(db) => _release = release;

    // POST /api/bookings — reserve a slot and pay (payment is simulated)
    [Authorize(Roles = "user,admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest? body)
    {
        await _release.ReleaseExpiredBookingsAsync();

        if (body?.SlotId == null || !TryParseIso(body.StartTime, out var start) || !TryParseIso(body.EndTime, out var end) ||
            (body.PaymentMethod != null && !PaymentMethods.Contains(body.PaymentMethod)))
            return Error(400, "Please provide a valid slot and time range.");

        if (!(start < end)) return Error(400, "End time must be after start time.");
        if (start < DateTime.UtcNow.AddMinutes(-5)) return Error(400, "Start time cannot be in the past.");

        await using var conn = await Db.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        // Lock the slot row (SELECT ... FOR UPDATE). A second request for the same slot waits
        // here until this transaction commits, so the overlap check below can't race — this
        // closes the "booking concurrency" risk from the report's Critical Analysis (Ch. 8).
        var slot = await conn.QuerySingleOrDefaultAsync<Slot>(
            "SELECT * FROM slots WHERE id = @id FOR UPDATE", new { id = body.SlotId }, tx);
        if (slot == null) return Error(404, "Slot not found.");
        if (slot.Status == "maintenance") return Error(409, "This slot is currently under maintenance.");

        var pricePerHour = await conn.ExecuteScalarAsync<decimal>(
            "SELECT price_per_hour FROM parking_areas WHERE id = @id", new { id = slot.ParkingAreaId }, tx);

        var overlap = await conn.ExecuteScalarAsync<int?>(
            @"SELECT id FROM bookings
              WHERE slot_id = @slotId AND status = 'confirmed'
                AND NOT (end_time <= @start OR start_time >= @end)
              LIMIT 1",
            new { slotId = slot.Id, start, end }, tx);
        if (overlap != null) return Error(409, "This slot is already booked for part of the selected time range.");

        var hours = Math.Max(1, (int)Math.Ceiling((end - start).TotalHours));
        var totalAmount = Math.Round(hours * pricePerHour, 2, MidpointRounding.AwayFromZero);

        var bookingId = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO bookings (user_id, slot_id, start_time, end_time, status, total_amount, vehicle_plate)
              VALUES (@userId, @slotId, @start, @end, 'pending_payment', @totalAmount, @plate);
              SELECT LAST_INSERT_ID();",
            new
            {
                userId = CurrentUserId, slotId = slot.Id, start, end, totalAmount,
                plate = string.IsNullOrWhiteSpace(body.VehiclePlate) ? null : body.VehiclePlate.Trim(),
            }, tx);

        // Simulated payment gateway — always succeeds in this build. A real eSewa/Khalti
        // integration would call the gateway here and only confirm after a verified callback.
        var txnRef = $"TXN-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(3))}";
        await conn.ExecuteAsync(
            @"INSERT INTO payments (booking_id, amount, method, status, transaction_ref)
              VALUES (@bookingId, @totalAmount, @method, 'success', @txnRef)",
            new { bookingId, totalAmount, method = body.PaymentMethod ?? "card", txnRef }, tx);

        await conn.ExecuteAsync("UPDATE bookings SET status = 'confirmed' WHERE id = @bookingId", new { bookingId }, tx);
        await conn.ExecuteAsync("UPDATE slots SET status = 'booked' WHERE id = @id", new { id = slot.Id }, tx);

        await tx.CommitAsync();

        var booking = await conn.QuerySingleAsync<BookingDetail>(BookingDetailSql + " WHERE b.id = @bookingId", new { bookingId });
        return StatusCode(201, new { booking });
    }

    // GET /api/bookings/me — booking history for the logged-in user
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Mine()
    {
        await _release.ReleaseExpiredBookingsAsync();
        await using var conn = await Db.OpenAsync();
        var bookings = await conn.QueryAsync<BookingDetail>(
            BookingDetailSql + " WHERE b.user_id = @userId ORDER BY b.created_at DESC, b.id DESC", new { userId = CurrentUserId });
        return Ok(new { bookings });
    }

    // PUT /api/bookings/{id}/cancel — booking owner, the area's owner, or an admin can cancel
    [Authorize]
    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> Cancel(int id)
    {
        await _release.ReleaseExpiredBookingsAsync();
        await using var conn = await Db.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var booking = await conn.QuerySingleOrDefaultAsync<Booking>("SELECT * FROM bookings WHERE id = @id FOR UPDATE", new { id }, tx);
        if (booking == null) return Error(404, "Booking not found.");

        var areaOwnerId = await conn.ExecuteScalarAsync<int?>(
            @"SELECT pa.owner_id FROM slots s JOIN parking_areas pa ON pa.id = s.parking_area_id WHERE s.id = @slotId",
            new { slotId = booking.SlotId }, tx);

        if (!IsAdmin && booking.UserId != CurrentUserId && areaOwnerId != CurrentUserId)
            return Error(403, "You can only cancel your own bookings.");
        if (booking.Status is not ("confirmed" or "pending_payment"))
            return Error(409, $"Booking is already {booking.Status} and cannot be cancelled.");

        await conn.ExecuteAsync("UPDATE bookings SET status = 'cancelled' WHERE id = @id", new { id }, tx);
        await conn.ExecuteAsync("UPDATE slots SET status = 'available' WHERE id = @slotId", new { slotId = booking.SlotId }, tx);
        await tx.CommitAsync();

        var updated = await conn.QuerySingleAsync<BookingDetail>(BookingDetailSql + " WHERE b.id = @id", new { id });
        return Ok(new { booking = updated });
    }

    // GET /api/bookings?areaId=&status= — owner sees bookings for their areas, admin sees all
    [Authorize(Roles = "parking_owner,admin")]
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? areaId, [FromQuery] string? status)
    {
        await _release.ReleaseExpiredBookingsAsync();

        var sql = BookingDetailSql + " WHERE 1=1";
        var p = new DynamicParameters();
        if (!IsAdmin)
        {
            sql += " AND pa.owner_id = @ownerId";
            p.Add("ownerId", CurrentUserId);
        }
        if (areaId != null)
        {
            sql += " AND pa.id = @areaId";
            p.Add("areaId", areaId);
        }
        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND b.status = @status";
            p.Add("status", status);
        }
        sql += " ORDER BY b.created_at DESC, b.id DESC";

        await using var conn = await Db.OpenAsync();
        var bookings = await conn.QueryAsync<BookingDetail>(sql, p);
        return Ok(new { bookings });
    }

    /// <summary>Parses an ISO 8601 timestamp (e.g. from JavaScript's toISOString()) into UTC.</summary>
    private static bool TryParseIso(string? value, out DateTime utc)
    {
        utc = default;
        if (string.IsNullOrWhiteSpace(value)) return false;
        if (!DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto)) return false;
        // DATETIME has whole-second precision; drop milliseconds so stored and compared values match.
        var t = dto.UtcDateTime;
        utc = new DateTime(t.Ticks - t.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        return true;
    }
}
