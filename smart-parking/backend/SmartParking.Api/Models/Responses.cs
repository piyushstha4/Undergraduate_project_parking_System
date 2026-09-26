using System.Text.Json.Serialization;

namespace SmartParking.Api.Models;

// A few responses in the original API used camelCase keys (totalUsers, avgRating, ...).
// [JsonPropertyName] overrides the global snake_case policy so the React pages keep working.

public record ErrorResponse(string Error);

public record AuthResponse(string Token, User User);

public class AreaDetailResponse
{
    public ParkingArea Area { get; set; } = null!;
    public List<Slot> Slots { get; set; } = new();
    public List<ReviewRow> Reviews { get; set; } = new();
    [JsonPropertyName("avgRating")] public double? AvgRating { get; set; }
}

public class StatusCount
{
    public string Status { get; set; } = "";
    public long Count { get; set; }
}

public class RecentBooking
{
    public int Id { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string UserName { get; set; } = "";
    public string AreaName { get; set; } = "";
}

public class AdminStatsResponse
{
    [JsonPropertyName("totalUsers")] public long TotalUsers { get; set; }
    [JsonPropertyName("totalOwners")] public long TotalOwners { get; set; }
    [JsonPropertyName("totalAreas")] public long TotalAreas { get; set; }
    [JsonPropertyName("totalSlots")] public long TotalSlots { get; set; }
    [JsonPropertyName("availableSlots")] public long AvailableSlots { get; set; }
    [JsonPropertyName("activeBookings")] public long ActiveBookings { get; set; }
    [JsonPropertyName("totalBookings")] public long TotalBookings { get; set; }
    [JsonPropertyName("revenue")] public decimal Revenue { get; set; }
    [JsonPropertyName("bookingsByStatus")] public List<StatusCount> BookingsByStatus { get; set; } = new();
    [JsonPropertyName("recentBookings")] public List<RecentBooking> RecentBookings { get; set; } = new();
}

public class ReportArea
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public decimal PricePerHour { get; set; }
    public long TotalSlots { get; set; }
    public long AvailableSlots { get; set; }
    public long TotalBookings { get; set; }
    public long ActiveBookings { get; set; }
    public decimal Revenue { get; set; }
}

public class ReportTotals
{
    [JsonPropertyName("totalBookings")] public long TotalBookings { get; set; }
    [JsonPropertyName("activeBookings")] public long ActiveBookings { get; set; }
    [JsonPropertyName("revenue")] public decimal Revenue { get; set; }
    [JsonPropertyName("totalSlots")] public long TotalSlots { get; set; }
    [JsonPropertyName("availableSlots")] public long AvailableSlots { get; set; }
}

public record OwnerReportResponse(List<ReportArea> Areas, ReportTotals Totals);
