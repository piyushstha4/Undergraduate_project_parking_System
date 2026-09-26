using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>Reviews (was routes/reviews.js).</summary>
[Authorize(Roles = "user,admin")]
public class ReviewsController : ApiControllerBase
{
    public ReviewsController(Db db) : base(db) { }

    // POST /api/parking-areas/{areaId}/reviews — only users who booked the area can review it
    [HttpPost("api/parking-areas/{areaId:int}/reviews")]
    public async Task<IActionResult> Create(int areaId, [FromBody] ReviewRequest? body)
    {
        if (body?.Rating is not (>= 1 and <= 5)) return Error(400, "Rating must be between 1 and 5.");

        await using var conn = await Db.OpenAsync();
        var exists = await conn.ExecuteScalarAsync<int?>("SELECT id FROM parking_areas WHERE id = @areaId", new { areaId });
        if (exists == null) return Error(404, "Parking area not found.");

        var hasBooking = await conn.ExecuteScalarAsync<int?>(
            @"SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
              WHERE s.parking_area_id = @areaId AND b.user_id = @userId AND b.status IN ('confirmed','completed') LIMIT 1",
            new { areaId, userId = CurrentUserId });
        if (hasBooking == null) return Error(403, "You can only review parking areas you have booked.");

        await conn.ExecuteAsync(
            "INSERT INTO reviews (user_id, parking_area_id, rating, comment) VALUES (@userId, @areaId, @rating, @comment)",
            new { userId = CurrentUserId, areaId, rating = body.Rating, comment = string.IsNullOrEmpty(body.Comment) ? null : body.Comment });
        return StatusCode(201, new { success = true });
    }
}
