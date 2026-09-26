using Newtonsoft.Json;

namespace SmartParking.Api.Models
{
    public sealed class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
    }

    public sealed class AuthResponse
    {
        public string Token { get; set; }
        public UserDto User { get; set; }
    }

    public sealed class RegisterRequest
    {
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
    }

    public sealed class LoginRequest
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }

    public sealed class ParkingAreaDto
    {
        public int Id { get; set; }
        public int OwnerId { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Description { get; set; }
        public double PricePerHour { get; set; }
        public string OpeningTime { get; set; }
        public string ClosingTime { get; set; }
        public string VehicleTypes { get; set; }
        public string CreatedAt { get; set; }
        public int AvailableSlots { get; set; }
        public int TotalSlots { get; set; }
        public string OwnerName { get; set; }
        public string OwnerEmail { get; set; }
    }

    public sealed class CreateParkingAreaRequest
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string Description { get; set; }
        public double? PricePerHour { get; set; }
        public string OpeningTime { get; set; }
        public string ClosingTime { get; set; }
        public string VehicleTypes { get; set; }
    }

    public sealed class SlotDto
    {
        public int Id { get; set; }
        public int ParkingAreaId { get; set; }
        public string SlotNumber { get; set; }
        public string VehicleType { get; set; }
        public string Status { get; set; }
        public string CreatedAt { get; set; }
    }

    public sealed class CreateSlotRequest
    {
        public string SlotNumber { get; set; }
        public string VehicleType { get; set; }
    }

    public sealed class UpdateSlotRequest
    {
        public string SlotNumber { get; set; }
        public string VehicleType { get; set; }
        public string Status { get; set; }
    }

    public sealed class ReviewDto
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string CreatedAt { get; set; }
        public string UserName { get; set; }
    }

    public sealed class CreateReviewRequest
    {
        public int Rating { get; set; }
        public string Comment { get; set; }
    }

    public sealed class AreaDetailResponse
    {
        public ParkingAreaDto Area { get; set; }
        public System.Collections.Generic.List<SlotDto> Slots { get; set; }
        public System.Collections.Generic.List<ReviewDto> Reviews { get; set; }

        [JsonProperty("avgRating")]
        public double? AvgRating { get; set; }
    }

    public sealed class CreateBookingRequest
    {
        public int SlotId { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string VehiclePlate { get; set; }
        public string PaymentMethod { get; set; }
    }

    public sealed class BookingDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int SlotId { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string Status { get; set; }
        public double TotalAmount { get; set; }
        public string VehiclePlate { get; set; }
        public string CreatedAt { get; set; }
        public string SlotNumber { get; set; }
        public string VehicleType { get; set; }
        public string AreaName { get; set; }
        public string AreaAddress { get; set; }
        public string AreaCity { get; set; }
        public int AreaId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
    }

    public sealed class StatusUpdateRequest
    {
        public string Status { get; set; }
    }

    public sealed class StatusCountDto
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public sealed class RecentBookingDto
    {
        public int Id { get; set; }
        public string Status { get; set; }
        public double TotalAmount { get; set; }
        public string CreatedAt { get; set; }
        public string UserName { get; set; }
        public string AreaName { get; set; }
    }

    public sealed class AdminStatsDto
    {
        [JsonProperty("totalUsers")]
        public int TotalUsers { get; set; }

        [JsonProperty("totalOwners")]
        public int TotalOwners { get; set; }

        [JsonProperty("totalAreas")]
        public int TotalAreas { get; set; }

        [JsonProperty("totalSlots")]
        public int TotalSlots { get; set; }

        [JsonProperty("availableSlots")]
        public int AvailableSlots { get; set; }

        [JsonProperty("activeBookings")]
        public int ActiveBookings { get; set; }

        [JsonProperty("totalBookings")]
        public int TotalBookings { get; set; }

        [JsonProperty("revenue")]
        public double Revenue { get; set; }

        [JsonProperty("bookingsByStatus")]
        public System.Collections.Generic.List<StatusCountDto> BookingsByStatus { get; set; }

        [JsonProperty("recentBookings")]
        public System.Collections.Generic.List<RecentBookingDto> RecentBookings { get; set; }
    }

    public sealed class OwnerAreaReportDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public double PricePerHour { get; set; }
        public int TotalSlots { get; set; }
        public int AvailableSlots { get; set; }
        public int TotalBookings { get; set; }
        public int ActiveBookings { get; set; }
        public double Revenue { get; set; }
    }

    public sealed class OwnerTotalsDto
    {
        [JsonProperty("totalBookings")]
        public int TotalBookings { get; set; }

        [JsonProperty("activeBookings")]
        public int ActiveBookings { get; set; }

        [JsonProperty("revenue")]
        public double Revenue { get; set; }

        [JsonProperty("totalSlots")]
        public int TotalSlots { get; set; }

        [JsonProperty("availableSlots")]
        public int AvailableSlots { get; set; }
    }

    public sealed class OwnerReportResponse
    {
        public System.Collections.Generic.List<OwnerAreaReportDto> Areas { get; set; }
        public OwnerTotalsDto Totals { get; set; }
    }
}
