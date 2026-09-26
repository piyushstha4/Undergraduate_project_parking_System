using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>Owner usage/revenue reports (was routes/owner.js).</summary>
[Authorize(Roles = "parking_owner,admin")]
[Route("api/owner")]
public class OwnerController : ApiControllerBase
{
    public OwnerController(Db db) : base(db) { }

    // GET /api/owner/reports — admin sees every area, owners see their own
    [HttpGet("reports")]
    public async Task<IActionResult> Reports()
    {
        var ownerFilter = IsAdmin ? "" : "WHERE pa.owner_id = @ownerId";

        await using var conn = await Db.OpenAsync();
        var areas = (await conn.QueryAsync<ReportArea>($@"
            SELECT pa.id, pa.name, pa.city, pa.price_per_hour,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots,
              (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id WHERE s.parking_area_id = pa.id) AS total_bookings,
              (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id
                 WHERE s.parking_area_id = pa.id AND b.status = 'confirmed') AS active_bookings,
              (SELECT COALESCE(SUM(b.total_amount), 0) FROM bookings b JOIN slots s ON s.id = b.slot_id
                 WHERE s.parking_area_id = pa.id AND b.status IN ('confirmed','completed')) AS revenue
            FROM parking_areas pa {ownerFilter}
            ORDER BY revenue DESC, pa.id", new { ownerId = CurrentUserId })).ToList();

        var totals = new ReportTotals
        {
            TotalBookings = areas.Sum(a => a.TotalBookings),
            ActiveBookings = areas.Sum(a => a.ActiveBookings),
            Revenue = areas.Sum(a => a.Revenue),
            TotalSlots = areas.Sum(a => a.TotalSlots),
            AvailableSlots = areas.Sum(a => a.AvailableSlots),
        };

        return Ok(new OwnerReportResponse(areas, totals));
    }
}
