using System;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class AdminService
    {
        private readonly ParkingDb _db;

        public AdminService(ParkingDb db)
        {
            _db = db;
        }

        public AdminStatsDto Stats()
        {
            int Count(string sql) => Convert.ToInt32(_db.Scalar(sql));
            double Sum(string sql) => Convert.ToDouble(_db.Scalar(sql));

            return new AdminStatsDto
            {
                TotalUsers = Count("SELECT COUNT(*) FROM users WHERE role = 'user'"),
                TotalOwners = Count("SELECT COUNT(*) FROM users WHERE role = 'parking_owner'"),
                TotalAreas = Count("SELECT COUNT(*) FROM parking_areas"),
                TotalSlots = Count("SELECT COUNT(*) FROM slots"),
                AvailableSlots = Count("SELECT COUNT(*) FROM slots WHERE status = 'available'"),
                ActiveBookings = Count("SELECT COUNT(*) FROM bookings WHERE status = 'confirmed'"),
                TotalBookings = Count("SELECT COUNT(*) FROM bookings"),
                Revenue = Sum("SELECT COALESCE(SUM(amount),0) FROM payments WHERE status = 'success'"),
                BookingsByStatus = _db.Query(
                    "SELECT status, COUNT(*) AS count FROM bookings GROUP BY status",
                    r => new StatusCountDto
                    {
                        Status = Reader.String(r, "status"),
                        Count = Reader.Int(r, "count")
                    }),
                RecentBookings = _db.Query(
                    @"SELECT b.id, b.status, b.total_amount, b.created_at, u.name AS user_name, pa.name AS area_name
                      FROM bookings b
                      JOIN users u ON u.id = b.user_id
                      JOIN slots s ON s.id = b.slot_id
                      JOIN parking_areas pa ON pa.id = s.parking_area_id
                      ORDER BY b.created_at DESC LIMIT 8",
                    r => new RecentBookingDto
                    {
                        Id = Reader.Int(r, "id"),
                        Status = Reader.String(r, "status"),
                        TotalAmount = Reader.Double(r, "total_amount"),
                        CreatedAt = Reader.String(r, "created_at"),
                        UserName = Reader.String(r, "user_name"),
                        AreaName = Reader.String(r, "area_name")
                    })
            };
        }

        public System.Collections.Generic.List<UserDto> Users(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                return _db.Query(
                    "SELECT id, name, email, phone, role, status, created_at FROM users WHERE role != 'admin' ORDER BY created_at DESC",
                    Maps.User);
            }

            return _db.Query(
                "SELECT id, name, email, phone, role, status, created_at FROM users WHERE role != 'admin' AND role = @p0 ORDER BY created_at DESC",
                Maps.User,
                role);
        }

        public UserDto UpdateStatus(int userId, string status)
        {
            if (status != "active" && status != "suspended")
            {
                throw ApiException.BadRequest("Status must be \"active\" or \"suspended\".");
            }

            var user = _db.QuerySingle("SELECT * FROM users WHERE id = @p0", Maps.User, userId);
            if (user == null) throw ApiException.NotFound("User not found.");
            if (user.Role == "admin") throw ApiException.Forbidden("Cannot modify an admin account.");

            _db.Execute("UPDATE users SET status = @p0 WHERE id = @p1", status, userId);
            return _db.QuerySingle(
                "SELECT id, name, email, phone, role, status, created_at FROM users WHERE id = @p0",
                Maps.User,
                userId);
        }

        public System.Collections.Generic.List<ParkingAreaDto> ParkingAreas()
        {
            return _db.Query(
                Maps.AreaWithAvailabilitySql.Replace("FROM parking_areas pa",
                    @" , u.name AS owner_name, u.email AS owner_email
                       FROM parking_areas pa JOIN users u ON u.id = pa.owner_id")
                + " ORDER BY pa.created_at DESC",
                Maps.Area);
        }
    }
}
