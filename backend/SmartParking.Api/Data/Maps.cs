using System.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Data
{
    public static class Maps
    {
        public static UserDto User(IDataRecord row) => new UserDto
        {
            Id = Reader.Int(row, "id"),
            Name = Reader.String(row, "name"),
            Email = Reader.String(row, "email"),
            Phone = Reader.String(row, "phone"),
            Role = Reader.String(row, "role"),
            Status = Reader.String(row, "status"),
            CreatedAt = Reader.String(row, "created_at")
        };

        public static ParkingAreaDto Area(IDataRecord row)
        {
            var dto = new ParkingAreaDto
            {
                Id = Reader.Int(row, "id"),
                OwnerId = Reader.Int(row, "owner_id"),
                Name = Reader.String(row, "name"),
                Address = Reader.String(row, "address"),
                City = Reader.String(row, "city"),
                Latitude = Reader.DoubleOrNull(row, "latitude"),
                Longitude = Reader.DoubleOrNull(row, "longitude"),
                Description = Reader.String(row, "description"),
                PricePerHour = Reader.Double(row, "price_per_hour"),
                OpeningTime = Reader.String(row, "opening_time"),
                ClosingTime = Reader.String(row, "closing_time"),
                VehicleTypes = Reader.String(row, "vehicle_types"),
                CreatedAt = Reader.String(row, "created_at")
            };

            TryMap(row, "available_slots", () => dto.AvailableSlots = Reader.Int(row, "available_slots"));
            TryMap(row, "total_slots", () => dto.TotalSlots = Reader.Int(row, "total_slots"));
            TryMap(row, "owner_name", () => dto.OwnerName = Reader.String(row, "owner_name"));
            TryMap(row, "owner_email", () => dto.OwnerEmail = Reader.String(row, "owner_email"));
            return dto;
        }

        public static SlotDto Slot(IDataRecord row) => new SlotDto
        {
            Id = Reader.Int(row, "id"),
            ParkingAreaId = Reader.Int(row, "parking_area_id"),
            SlotNumber = Reader.String(row, "slot_number"),
            VehicleType = Reader.String(row, "vehicle_type"),
            Status = Reader.String(row, "status"),
            CreatedAt = Reader.String(row, "created_at")
        };

        public static ReviewDto Review(IDataRecord row) => new ReviewDto
        {
            Id = Reader.Int(row, "id"),
            Rating = Reader.Int(row, "rating"),
            Comment = Reader.String(row, "comment"),
            CreatedAt = Reader.String(row, "created_at"),
            UserName = Reader.String(row, "user_name")
        };

        public static BookingDto Booking(IDataRecord row) => new BookingDto
        {
            Id = Reader.Int(row, "id"),
            UserId = Reader.Int(row, "user_id"),
            SlotId = Reader.Int(row, "slot_id"),
            StartTime = Reader.String(row, "start_time"),
            EndTime = Reader.String(row, "end_time"),
            Status = Reader.String(row, "status"),
            TotalAmount = Reader.Double(row, "total_amount"),
            VehiclePlate = Reader.String(row, "vehicle_plate"),
            CreatedAt = Reader.String(row, "created_at"),
            SlotNumber = Reader.String(row, "slot_number"),
            VehicleType = Reader.String(row, "vehicle_type"),
            AreaName = Reader.String(row, "area_name"),
            AreaAddress = Reader.String(row, "area_address"),
            AreaCity = Reader.String(row, "area_city"),
            AreaId = Reader.Int(row, "area_id"),
            UserName = Reader.String(row, "user_name"),
            UserEmail = Reader.String(row, "user_email")
        };

        public const string BookingDetailSql = @"
            SELECT b.*, s.slot_number, s.vehicle_type, pa.name AS area_name, pa.address AS area_address,
                   pa.city AS area_city, pa.id AS area_id, u.name AS user_name, u.email AS user_email
            FROM bookings b
            JOIN slots s ON s.id = b.slot_id
            JOIN parking_areas pa ON pa.id = s.parking_area_id
            JOIN users u ON u.id = b.user_id";

        public const string AreaWithAvailabilitySql = @"
            SELECT pa.*,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
              (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots
            FROM parking_areas pa";

        private static void TryMap(IDataRecord row, string column, System.Action assign)
        {
            try
            {
                var _ = row[column];
                assign();
            }
            catch (System.IndexOutOfRangeException)
            {
            }
        }
    }
}
