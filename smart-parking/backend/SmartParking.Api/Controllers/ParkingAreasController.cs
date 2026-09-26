using System.Globalization;
using System.Text.Json;
using Dapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartParking.Api.Auth;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Controllers;

/// <summary>Parking areas + Search &amp; Filtering module (was routes/parkingAreas.js).</summary>
[Route("api/parking-areas")]
public class ParkingAreasController : ApiControllerBase
{
    public ParkingAreasController(Db db) : base(db) { }

    // Area columns plus live slot counts (withAvailability() in the Node version).
    internal const string AreaSelect = @"
        SELECT pa.*,
          (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots,
          (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots
        FROM parking_areas pa";

    // GET /api/parking-areas?q=&city=&maxPrice=&vehicleType=&onlyAvailable=true&sort=price_asc|price_desc|availability
    [HttpGet]
    public async Task<IActionResult> Search(
        [FromQuery] string? q, [FromQuery] string? city, [FromQuery] string? maxPrice,
        [FromQuery] string? vehicleType, [FromQuery] string? onlyAvailable, [FromQuery] string? sort)
    {
        var sql = AreaSelect + " WHERE 1=1";
        var p = new DynamicParameters();

        if (!string.IsNullOrEmpty(q))
        {
            sql += " AND (pa.name LIKE @q OR pa.address LIKE @q OR pa.city LIKE @q)";
            p.Add("q", $"%{q}%");
        }
        if (!string.IsNullOrEmpty(city))
        {
            sql += " AND pa.city LIKE @city";
            p.Add("city", $"%{city}%");
        }
        if (!string.IsNullOrEmpty(maxPrice) &&
            decimal.TryParse(maxPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var max))
        {
            sql += " AND pa.price_per_hour <= @maxPrice";
            p.Add("maxPrice", max);
        }
        if (!string.IsNullOrEmpty(vehicleType))
        {
            sql += " AND pa.vehicle_types LIKE @vehicleType";
            p.Add("vehicleType", $"%{vehicleType}%");
        }
        sql += " ORDER BY pa.id";

        await using var conn = await Db.OpenAsync();
        IEnumerable<ParkingArea> areas = await conn.QueryAsync<ParkingArea>(sql, p);

        if (onlyAvailable == "true") areas = areas.Where(a => a.AvailableSlots > 0);

        areas = sort switch
        {
            "price_asc" => areas.OrderBy(a => a.PricePerHour),
            "price_desc" => areas.OrderByDescending(a => a.PricePerHour),
            "availability" => areas.OrderByDescending(a => a.AvailableSlots),
            _ => areas,
        };

        return Ok(new { areas = areas.ToList() });
    }

    // GET /api/parking-areas/{id} — area + slots + reviews
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        await using var conn = await Db.OpenAsync();
        var area = await conn.QuerySingleOrDefaultAsync<ParkingArea>(AreaSelect + " WHERE pa.id = @id", new { id });
        if (area == null) return Error(404, "Parking area not found.");

        var slots = (await conn.QueryAsync<Slot>(
            "SELECT * FROM slots WHERE parking_area_id = @id ORDER BY slot_number", new { id })).ToList();
        var reviews = (await conn.QueryAsync<ReviewRow>(
            @"SELECT r.id, r.rating, r.comment, r.created_at, u.name AS user_name
              FROM reviews r JOIN users u ON u.id = r.user_id
              WHERE r.parking_area_id = @id ORDER BY r.created_at DESC, r.id DESC", new { id })).ToList();

        double? avg = reviews.Count > 0 ? Math.Round(reviews.Average(r => r.Rating), 1, MidpointRounding.AwayFromZero) : null;

        return Ok(new AreaDetailResponse { Area = area, Slots = slots, Reviews = reviews, AvgRating = avg });
    }

    // POST /api/parking-areas — owner creates a new facility
    [Authorize(Roles = "parking_owner,admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAreaRequest? body)
    {
        if (body == null ||
            string.IsNullOrWhiteSpace(body.Name) || string.IsNullOrWhiteSpace(body.Address) ||
            string.IsNullOrWhiteSpace(body.City) || body.PricePerHour is null or < 0)
            return Error(400, "Please fill all required fields correctly.");
        if (body.Capacity is null or < 1 or > 500)
            return Error(400, "Capacity must be between 1 and 500 vehicles.");

        await using var conn = await Db.OpenAsync();
        int ownerId;
        if (IsAdmin)
        {
            if (body.OwnerId == null) return Error(400, "Choose the parking owner for this area.");
            var ownerRole = await conn.ExecuteScalarAsync<string?>(
                "SELECT role FROM users WHERE id = @id AND status = 'active'", new { id = body.OwnerId });
            if (ownerRole != AppClaims.Owner) return Error(400, "The selected user is not an active parking owner.");
            ownerId = body.OwnerId.Value;
        }
        else
        {
            ownerId = CurrentUserId;
        }

        var vehicleTypes = string.IsNullOrEmpty(body.VehicleTypes) ? "car,bike" : body.VehicleTypes;
        await using var tx = await conn.BeginTransactionAsync();
        var id = await conn.ExecuteScalarAsync<long>(
            @"INSERT INTO parking_areas
                (owner_id, name, address, city, latitude, longitude, description, price_per_hour, capacity, opening_time, closing_time, vehicle_types)
              VALUES (@ownerId, @name, @address, @city, @latitude, @longitude, @description, @price, @capacity, @opening, @closing, @vehicleTypes);
              SELECT LAST_INSERT_ID();",
            new
            {
                ownerId,
                name = body.Name.Trim(),
                address = body.Address.Trim(),
                city = body.City.Trim(),
                latitude = body.Latitude,
                longitude = body.Longitude,
                description = string.IsNullOrEmpty(body.Description) ? null : body.Description,
                price = body.PricePerHour,
                capacity = body.Capacity,
                opening = string.IsNullOrEmpty(body.OpeningTime) ? "06:00" : body.OpeningTime,
                closing = string.IsNullOrEmpty(body.ClosingTime) ? "22:00" : body.ClosingTime,
                vehicleTypes,
            }, tx);

        foreach (var slot in BuildSlots(body.Capacity.Value, vehicleTypes))
        {
            await conn.ExecuteAsync(
                "INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (@areaId, @number, @type)",
                new { areaId = id, slot.number, slot.type }, tx);
        }

        await tx.CommitAsync();
        var area = await conn.QuerySingleAsync<ParkingArea>(AreaSelect + " WHERE pa.id = @id", new { id });
        return StatusCode(201, new { area });
    }

    // Columns an owner may update, with how to read each JSON value.
    private static readonly Dictionary<string, Func<JsonElement, object?>> UpdatableFields = new()
    {
        ["name"] = ReadString,
        ["address"] = ReadString,
        ["city"] = ReadString,
        ["latitude"] = e => ReadNumber(e) is { } d ? (double)d : null,
        ["longitude"] = e => ReadNumber(e) is { } d ? (double)d : null,
        ["description"] = ReadString,
        ["price_per_hour"] = e => ReadNumber(e) ?? throw new FormatException(),
        ["opening_time"] = ReadString,
        ["closing_time"] = ReadString,
        ["vehicle_types"] = ReadString,
    };

    // PUT /api/parking-areas/{id} — partial update; only fields present in the body change
    [Authorize(Roles = "parking_owner,admin")]
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] JsonElement body)
    {
        await using var conn = await Db.OpenAsync();
        var area = await conn.QuerySingleOrDefaultAsync<ParkingArea>("SELECT * FROM parking_areas WHERE id = @id", new { id });
        if (area == null) return Error(404, "Parking area not found.");
        if (!IsAdmin && area.OwnerId != CurrentUserId) return Error(403, "You can only edit your own parking areas.");

        var sets = new List<string>();
        var p = new DynamicParameters(new { id });
        if (body.ValueKind == JsonValueKind.Object)
        {
            foreach (var (column, read) in UpdatableFields)
            {
                if (!body.TryGetProperty(column, out var value)) continue;
                try { p.Add(column, read(value)); }
                catch (Exception e) when (e is FormatException or InvalidOperationException or OverflowException)
                {
                    return Error(400, $"Invalid value for {column}.");
                }
                sets.Add($"{column} = @{column}"); // column names come from the whitelist above, never from the client
            }
        }
        if (sets.Count == 0) return Error(400, "No fields to update.");

        await conn.ExecuteAsync($"UPDATE parking_areas SET {string.Join(", ", sets)} WHERE id = @id", p);
        var updated = await conn.QuerySingleAsync<ParkingArea>(AreaSelect + " WHERE pa.id = @id", new { id });
        return Ok(new { area = updated });
    }

    // DELETE /api/parking-areas/{id}
    [Authorize(Roles = "parking_owner,admin")]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await using var conn = await Db.OpenAsync();
        var area = await conn.QuerySingleOrDefaultAsync<ParkingArea>("SELECT * FROM parking_areas WHERE id = @id", new { id });
        if (area == null) return Error(404, "Parking area not found.");
        if (!IsAdmin && area.OwnerId != CurrentUserId) return Error(403, "You can only delete your own parking areas.");

        var activeBooking = await conn.ExecuteScalarAsync<int?>(
            @"SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
              WHERE s.parking_area_id = @id AND b.status = 'confirmed' LIMIT 1", new { id });
        if (activeBooking != null) return Error(409, "Cannot delete a parking area with active bookings.");

        await conn.ExecuteAsync("DELETE FROM parking_areas WHERE id = @id", new { id });
        return Ok(new { success = true });
    }

    // GET /api/parking-areas/owner/mine — owner's own facilities
    [Authorize(Roles = "parking_owner,admin")]
    [HttpGet("owner/mine")]
    public async Task<IActionResult> Mine()
    {
        await using var conn = await Db.OpenAsync();
        var areas = await conn.QueryAsync<ParkingArea>(
            AreaSelect + " WHERE pa.owner_id = @ownerId ORDER BY pa.id", new { ownerId = CurrentUserId });
        return Ok(new { areas });
    }

    /// <summary>One slot per vehicle the area can hold, split between car and bike when both are allowed.</summary>
    internal static IEnumerable<(string number, string type)> BuildSlots(int capacity, string vehicleTypes)
    {
        var allowsCar = vehicleTypes.Contains("car", StringComparison.OrdinalIgnoreCase);
        var allowsBike = vehicleTypes.Contains("bike", StringComparison.OrdinalIgnoreCase);
        if (!allowsCar && !allowsBike) allowsCar = true;

        var bikeCount = allowsBike && !allowsCar ? capacity
            : allowsCar && !allowsBike ? 0
            : capacity == 1 ? 0
            : Math.Max(1, (int)Math.Floor(capacity * 0.3));
        if (allowsCar && allowsBike && bikeCount >= capacity) bikeCount = capacity - 1;
        var carCount = capacity - bikeCount;

        for (var i = 1; i <= bikeCount; i++) yield return ($"B{i:D2}", "bike");
        for (var i = 1; i <= carCount; i++) yield return ($"A{i:D2}", "car");
    }

    private static object? ReadString(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.String => e.GetString(),
        JsonValueKind.Number => e.GetRawText(),
        _ => throw new FormatException(),
    };

    private static decimal? ReadNumber(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.Null => null,
        JsonValueKind.Number => e.GetDecimal(),
        JsonValueKind.String when string.IsNullOrWhiteSpace(e.GetString()) => null,
        JsonValueKind.String => decimal.Parse(e.GetString()!, NumberStyles.Number, CultureInfo.InvariantCulture),
        _ => throw new FormatException(),
    };
}
