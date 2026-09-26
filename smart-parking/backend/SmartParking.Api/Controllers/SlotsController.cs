using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>Parking Slot Management module (was routes/slots.js).</summary>
[Authorize(Roles = "parking_owner,admin")]
public class SlotsController : ApiControllerBase
{
    private static readonly string[] VehicleTypes = { "car", "bike" };
    private static readonly string[] SlotStatuses = { "available", "booked", "occupied", "maintenance" };

    public SlotsController(Db db) : base(db) { }

    // POST /api/parking-areas/{areaId}/slots — add a slot
    [HttpPost("api/parking-areas/{areaId:int}/slots")]
    public async Task<IActionResult> Create(int areaId, [FromBody] CreateSlotRequest? body)
    {
        var slotNumber = body?.SlotNumber?.Trim();
        if (string.IsNullOrEmpty(slotNumber)) return Error(400, "Slot number is required.");

        await using var conn = await Db.OpenAsync();
        var denied = await CheckOwnsArea(conn, areaId);
        if (denied != null) return denied;

        var capacity = await conn.ExecuteScalarAsync<int>(
            "SELECT capacity FROM parking_areas WHERE id = @areaId", new { areaId });
        var existing = await conn.ExecuteScalarAsync<long>(
            "SELECT COUNT(*) FROM slots WHERE parking_area_id = @areaId", new { areaId });
        if (existing >= capacity)
            return Error(409, $"This area is at its capacity of {capacity} vehicles.");

        try
        {
            var id = await conn.ExecuteScalarAsync<long>(
                @"INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (@areaId, @slotNumber, @vehicleType);
                  SELECT LAST_INSERT_ID();",
                new { areaId, slotNumber, vehicleType = body!.VehicleType == "bike" ? "bike" : "car" });
            var slot = await conn.QuerySingleAsync<Slot>("SELECT * FROM slots WHERE id = @id", new { id });
            return StatusCode(201, new { slot });
        }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(409, "A slot with this number already exists in this area.");
        }
    }

    // PUT /api/slots/{id} — update slot_number, vehicle_type and/or status
    [HttpPut("api/slots/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] Dictionary<string, string?>? body)
    {
        await using var conn = await Db.OpenAsync();
        var slot = await conn.QuerySingleOrDefaultAsync<Slot>("SELECT * FROM slots WHERE id = @id", new { id });
        if (slot == null) return Error(404, "Slot not found.");
        var denied = await CheckOwnsArea(conn, slot.ParkingAreaId);
        if (denied != null) return denied;

        body ??= new();
        body.TryGetValue("status", out var status);

        var activeBooking = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM bookings WHERE slot_id = @id AND status = 'confirmed' LIMIT 1", new { id });
        if (activeBooking != null && !string.IsNullOrEmpty(status) && status != "booked")
            return Error(409, "Cannot change status of a slot with an active booking.");

        var sets = new List<string>();
        var p = new DynamicParameters(new { id });

        if (body.TryGetValue("slot_number", out var slotNumber))
        {
            if (string.IsNullOrWhiteSpace(slotNumber)) return Error(400, "Slot number is required.");
            sets.Add("slot_number = @slot_number");
            p.Add("slot_number", slotNumber.Trim());
        }
        if (body.TryGetValue("vehicle_type", out var vehicleType))
        {
            if (!VehicleTypes.Contains(vehicleType)) return Error(400, "Vehicle type must be \"car\" or \"bike\".");
            sets.Add("vehicle_type = @vehicle_type");
            p.Add("vehicle_type", vehicleType);
        }
        if (body.ContainsKey("status"))
        {
            if (!SlotStatuses.Contains(status)) return Error(400, "Invalid slot status.");
            sets.Add("status = @status");
            p.Add("status", status);
        }
        if (sets.Count == 0) return Error(400, "No fields to update.");

        try
        {
            await conn.ExecuteAsync($"UPDATE slots SET {string.Join(", ", sets)} WHERE id = @id", p);
        }
        catch (MySqlException e) when (e.ErrorCode == MySqlErrorCode.DuplicateKeyEntry)
        {
            return Error(409, "A slot with this number already exists in this area.");
        }

        var updated = await conn.QuerySingleAsync<Slot>("SELECT * FROM slots WHERE id = @id", new { id });
        return Ok(new { slot = updated });
    }

    // DELETE /api/slots/{id}
    [HttpDelete("api/slots/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var conn = await Db.OpenAsync();
        var slot = await conn.QuerySingleOrDefaultAsync<Slot>("SELECT * FROM slots WHERE id = @id", new { id });
        if (slot == null) return Error(404, "Slot not found.");
        var denied = await CheckOwnsArea(conn, slot.ParkingAreaId);
        if (denied != null) return denied;

        var activeBooking = await conn.ExecuteScalarAsync<int?>(
            "SELECT id FROM bookings WHERE slot_id = @id AND status = 'confirmed' LIMIT 1", new { id });
        if (activeBooking != null) return Error(409, "Cannot delete a slot with an active booking.");

        await conn.ExecuteAsync("DELETE FROM slots WHERE id = @id", new { id });
        return Ok(new { success = true });
    }

    /// <summary>Returns an error result if the area doesn't exist or isn't owned by the caller (admins can manage all).</summary>
    private async Task<IActionResult?> CheckOwnsArea(MySqlConnection conn, int areaId)
    {
        var ownerId = await conn.ExecuteScalarAsync<int?>("SELECT owner_id FROM parking_areas WHERE id = @areaId", new { areaId });
        if (ownerId == null) return Error(404, "Parking area not found.");
        if (!IsAdmin && ownerId != CurrentUserId) return Error(403, "You can only manage slots in your own parking areas.");
        return null;
    }
}
