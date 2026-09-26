using System.Text.Json.Serialization;

namespace SmartParking.Api.Models;

// Row classes. Dapper maps snake_case columns (e.g. price_per_hour) onto these
// PascalCase properties (MatchNamesWithUnderscores), and the JSON serializer writes
// them back out as snake_case, so the React app receives exactly the same field
// names the old Node API returned.

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Phone { get; set; }
    [JsonIgnore] public string PasswordHash { get; set; } = "";
    public string Role { get; set; } = "user";
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; }
}

public class ParkingArea
{
    public int Id { get; set; }
    public int OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Address { get; set; } = "";
    public string City { get; set; } = "";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public decimal PricePerHour { get; set; }
    public int Capacity { get; set; }
    public string OpeningTime { get; set; } = "06:00";
    public string ClosingTime { get; set; } = "22:00";
    public string VehicleTypes { get; set; } = "car,bike";
    public DateTime CreatedAt { get; set; }

    // Computed with sub-queries (see ParkingAreaQueries)
    public long AvailableSlots { get; set; }
    public long TotalSlots { get; set; }
}

/// <summary>Admin listing adds the owner's name/email.</summary>
public class AdminParkingArea : ParkingArea
{
    public string OwnerName { get; set; } = "";
    public string OwnerEmail { get; set; } = "";
}

public class Slot
{
    public int Id { get; set; }
    public int ParkingAreaId { get; set; }
    public string SlotNumber { get; set; } = "";
    public string VehicleType { get; set; } = "car";
    public string Status { get; set; } = "available";
    public DateTime CreatedAt { get; set; }
}

public class Booking
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int SlotId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string Status { get; set; } = "pending_payment";
    public decimal TotalAmount { get; set; }
    public string? VehiclePlate { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>Booking joined with slot, area and user info (BOOKING_DETAIL_SQL in the Node version).</summary>
public class BookingDetail : Booking
{
    public string SlotNumber { get; set; } = "";
    public string VehicleType { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string AreaAddress { get; set; } = "";
    public string AreaCity { get; set; } = "";
    public int AreaId { get; set; }
    public string UserName { get; set; } = "";
    public string UserEmail { get; set; } = "";
}

public class ReviewRow
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = "";
}
