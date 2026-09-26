namespace SmartParking.Api.Models;

// Request bodies. The JSON serializer is configured for snake_case, so e.g. the React
// app's "slot_id" binds to SlotId and "price_per_hour" binds to PricePerHour.

public class RegisterRequest
{
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Phone { get; set; }
    public string? Role { get; set; }
}

public class LoginRequest
{
    public string? Email { get; set; }
    public string? Password { get; set; }
}

public class CreateAreaRequest
{
    public string? Name { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? Description { get; set; }
    public decimal? PricePerHour { get; set; }
    public int? Capacity { get; set; }
    public int? OwnerId { get; set; }
    public string? OpeningTime { get; set; }
    public string? ClosingTime { get; set; }
    public string? VehicleTypes { get; set; }
}

public class CreateSlotRequest
{
    public string? SlotNumber { get; set; }
    public string? VehicleType { get; set; }
}

public class CreateBookingRequest
{
    public int? SlotId { get; set; }
    public string? StartTime { get; set; }   // ISO 8601, e.g. 2026-09-24T10:00:00.000Z
    public string? EndTime { get; set; }
    public string? VehiclePlate { get; set; }
    public string? PaymentMethod { get; set; } // esewa | khalti | card
}

public class ReviewRequest
{
    public int? Rating { get; set; }
    public string? Comment { get; set; }
}

public class UserStatusRequest
{
    public string? Status { get; set; }
}

public class SetPasswordRequest
{
    public string? Password { get; set; }
}
