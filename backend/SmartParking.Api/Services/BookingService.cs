using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class BookingService
    {
        private static readonly HashSet<string> PaymentMethods = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "esewa", "khalti", "card"
        };

        private readonly ParkingDb _db;

        public BookingService(ParkingDb db)
        {
            _db = db;
        }

        public int ReleaseExpired()
        {
            var now = DateTime.UtcNow.ToString("o");
            return _db.Write((connection, transaction) =>
            {
                var expired = new List<int>();
                var slotIds = new List<int>();
                using (var command = ParkingDb.Command(connection, transaction, "SELECT id, slot_id FROM bookings WHERE status = 'confirmed' AND end_time <= @p0", now))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        expired.Add(Convert.ToInt32(reader["id"]));
                        slotIds.Add(Convert.ToInt32(reader["slot_id"]));
                    }
                }

                for (var i = 0; i < expired.Count; i++)
                {
                    ParkingDb.Exec(connection, transaction, "UPDATE bookings SET status = 'completed' WHERE id = @p0", expired[i]);
                    ParkingDb.Exec(connection, transaction, "UPDATE slots SET status = 'available' WHERE id = @p0", slotIds[i]);
                }

                return expired.Count;
            });
        }

        public BookingDto Create(int userId, CreateBookingRequest request)
        {
            if (request == null || request.SlotId <= 0 ||
                !DateTime.TryParse(request.StartTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var start) ||
                !DateTime.TryParse(request.EndTime, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var end))
            {
                throw ApiException.BadRequest("Please provide a valid slot and time range.");
            }

            if (!(start < end)) throw ApiException.BadRequest("End time must be after start time.");
            var startUtc = start.Kind == DateTimeKind.Utc ? start : start.ToUniversalTime();
            if (startUtc < DateTime.UtcNow.AddMinutes(-5))
            {
                throw ApiException.BadRequest("Start time cannot be in the past.");
            }

            var method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "card" : request.PaymentMethod.ToLowerInvariant();
            if (!PaymentMethods.Contains(method))
            {
                throw ApiException.BadRequest("Please provide a valid slot and time range.");
            }

            var slot = _db.QuerySingle("SELECT * FROM slots WHERE id = @p0", Maps.Slot, request.SlotId);
            if (slot == null) throw ApiException.NotFound("Slot not found.");
            if (slot.Status == "maintenance")
            {
                throw ApiException.Conflict("This slot is currently under maintenance.");
            }

            var priceObj = _db.Scalar("SELECT price_per_hour FROM parking_areas WHERE id = @p0", slot.ParkingAreaId);
            var price = Convert.ToDouble(priceObj);

            try
            {
                var bookingId = _db.Write((connection, transaction) =>
                {
                    var overlap = ParkingDb.Scalar(
                        connection,
                        transaction,
                        @"SELECT id FROM bookings
                          WHERE slot_id = @p0 AND status = 'confirmed'
                            AND NOT (end_time <= @p1 OR start_time >= @p2)
                          LIMIT 1",
                        request.SlotId,
                        request.StartTime,
                        request.EndTime);
                    if (overlap != null && overlap != DBNull.Value)
                    {
                        throw ApiException.Conflict("This slot is already booked for part of the selected time range.");
                    }

                    var hours = Math.Max(1, (int)Math.Ceiling((end - start).TotalHours));
                    var total = Math.Round(hours * price, 2);

                    ParkingDb.Exec(
                        connection,
                        transaction,
                        @"INSERT INTO bookings (user_id, slot_id, start_time, end_time, status, total_amount, vehicle_plate)
                          VALUES (@p0, @p1, @p2, @p3, 'pending_payment', @p4, @p5)",
                        userId,
                        request.SlotId,
                        request.StartTime,
                        request.EndTime,
                        total,
                        string.IsNullOrWhiteSpace(request.VehiclePlate) ? null : request.VehiclePlate.Trim());

                    var id = ParkingDb.LastInsertId(connection, transaction);
                    var txnRef = "TXN-" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + RandomHex(3);

                    ParkingDb.Exec(
                        connection,
                        transaction,
                        @"INSERT INTO payments (booking_id, amount, method, status, transaction_ref)
                          VALUES (@p0, @p1, @p2, 'success', @p3)",
                        id,
                        total,
                        method,
                        txnRef);

                    ParkingDb.Exec(connection, transaction, "UPDATE bookings SET status = 'confirmed' WHERE id = @p0", id);
                    ParkingDb.Exec(connection, transaction, "UPDATE slots SET status = 'booked' WHERE id = @p0", request.SlotId);
                    return id;
                });

                return _db.QuerySingle(Maps.BookingDetailSql + " WHERE b.id = @p0", Maps.Booking, bookingId);
            }
            catch (ApiException)
            {
                throw;
            }
        }

        public List<BookingDto> ForUser(int userId)
        {
            ReleaseExpired();
            return _db.Query(Maps.BookingDetailSql + " WHERE b.user_id = @p0 ORDER BY b.created_at DESC", Maps.Booking, userId);
        }

        public List<BookingDto> ForOwnerOrAdmin(int userId, string role, string areaId, string status)
        {
            ReleaseExpired();
            var sql = new StringBuilder(Maps.BookingDetailSql + " WHERE 1=1");
            var args = new List<object>();

            if (role != "admin")
            {
                sql.Append(" AND pa.owner_id = @p").Append(args.Count);
                args.Add(userId);
            }

            if (!string.IsNullOrWhiteSpace(areaId))
            {
                sql.Append(" AND pa.id = @p").Append(args.Count);
                args.Add(Convert.ToInt32(areaId, CultureInfo.InvariantCulture));
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                sql.Append(" AND b.status = @p").Append(args.Count);
                args.Add(status);
            }

            sql.Append(" ORDER BY b.created_at DESC");
            return _db.Query(sql.ToString(), Maps.Booking, args.ToArray());
        }

        public BookingDto Cancel(int bookingId, int userId, string role)
        {
            var booking = _db.QuerySingle("SELECT * FROM bookings WHERE id = @p0", r => new
            {
                Id = Reader.Int(r, "id"),
                UserId = Reader.Int(r, "user_id"),
                SlotId = Reader.Int(r, "slot_id"),
                Status = Reader.String(r, "status")
            }, bookingId);

            if (booking == null) throw ApiException.NotFound("Booking not found.");

            var ownerIdObj = _db.Scalar(
                @"SELECT pa.owner_id FROM parking_areas pa
                  JOIN slots s ON s.parking_area_id = pa.id
                  WHERE s.id = @p0",
                booking.SlotId);
            var ownerId = ownerIdObj == null || ownerIdObj == DBNull.Value ? 0 : Convert.ToInt32(ownerIdObj);
            var isOwnerOfArea = ownerId == userId;

            if (role != "admin" && booking.UserId != userId && !isOwnerOfArea)
            {
                throw ApiException.Forbidden("You can only cancel your own bookings.");
            }

            if (booking.Status != "confirmed" && booking.Status != "pending_payment")
            {
                throw ApiException.Conflict("Booking is already " + booking.Status + " and cannot be cancelled.");
            }

            _db.Write((connection, transaction) =>
            {
                ParkingDb.Exec(connection, transaction, "UPDATE bookings SET status = 'cancelled' WHERE id = @p0", booking.Id);
                ParkingDb.Exec(connection, transaction, "UPDATE slots SET status = 'available' WHERE id = @p0", booking.SlotId);
                return 0;
            });

            return _db.QuerySingle(Maps.BookingDetailSql + " WHERE b.id = @p0", Maps.Booking, booking.Id);
        }

        private static string RandomHex(int bytes)
        {
            var buffer = new byte[bytes];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(buffer);
            }

            var sb = new StringBuilder(buffer.Length * 2);
            foreach (var b in buffer) sb.Append(b.ToString("X2"));
            return sb.ToString();
        }
    }
}
