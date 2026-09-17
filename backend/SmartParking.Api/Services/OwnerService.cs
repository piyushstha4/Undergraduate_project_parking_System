using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class OwnerService
    {
        private readonly ParkingDb _db;

        public OwnerService(ParkingDb db)
        {
            _db = db;
        }

        public OwnerReportResponse Reports(int userId, string role)
        {
            var sql = @"SELECT pa.id, pa.name, pa.city, pa.price_per_hour,
                (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
                (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots,
                (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id WHERE s.parking_area_id = pa.id) AS total_bookings,
                (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id WHERE s.parking_area_id = pa.id AND b.status = 'confirmed') AS active_bookings,
                (SELECT COALESCE(SUM(b.total_amount),0) FROM bookings b JOIN slots s ON s.id = b.slot_id
                   WHERE s.parking_area_id = pa.id AND b.status IN ('confirmed','completed')) AS revenue
               FROM parking_areas pa";

            var areas = role == "admin"
                ? _db.Query(sql + " ORDER BY revenue DESC", MapArea)
                : _db.Query(sql + " WHERE pa.owner_id = @p0 ORDER BY revenue DESC", MapArea, userId);

            var totals = new OwnerTotalsDto();
            foreach (var area in areas)
            {
                totals.TotalBookings += area.TotalBookings;
                totals.ActiveBookings += area.ActiveBookings;
                totals.Revenue += area.Revenue;
                totals.TotalSlots += area.TotalSlots;
                totals.AvailableSlots += area.AvailableSlots;
            }

            return new OwnerReportResponse { Areas = areas, Totals = totals };
        }

        private static OwnerAreaReportDto MapArea(System.Data.IDataRecord row) => new OwnerAreaReportDto
        {
            Id = Reader.Int(row, "id"),
            Name = Reader.String(row, "name"),
            City = Reader.String(row, "city"),
            PricePerHour = Reader.Double(row, "price_per_hour"),
            TotalSlots = Reader.Int(row, "total_slots"),
            AvailableSlots = Reader.Int(row, "available_slots"),
            TotalBookings = Reader.Int(row, "total_bookings"),
            ActiveBookings = Reader.Int(row, "active_bookings"),
            Revenue = Reader.Double(row, "revenue")
        };
    }
}
