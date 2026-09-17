using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class ParkingAreaService
    {
        private readonly ParkingDb _db;

        public ParkingAreaService(ParkingDb db)
        {
            _db = db;
        }

        public List<ParkingAreaDto> Search(string q, string city, string maxPrice, string vehicleType, string onlyAvailable, string sort)
        {
            var sql = new StringBuilder(Maps.AreaWithAvailabilitySql + " WHERE 1=1");
            var args = new List<object>();

            if (!string.IsNullOrWhiteSpace(q))
            {
                sql.Append(" AND (pa.name LIKE @p").Append(args.Count)
                    .Append(" OR pa.address LIKE @p").Append(args.Count)
                    .Append(" OR pa.city LIKE @p").Append(args.Count).Append(")");
                args.Add("%" + q.Trim() + "%");
            }

            if (!string.IsNullOrWhiteSpace(city))
            {
                sql.Append(" AND pa.city LIKE @p").Append(args.Count);
                args.Add("%" + city.Trim() + "%");
            }

            if (!string.IsNullOrWhiteSpace(maxPrice) && double.TryParse(maxPrice, NumberStyles.Any, CultureInfo.InvariantCulture, out var price))
            {
                sql.Append(" AND pa.price_per_hour <= @p").Append(args.Count);
                args.Add(price);
            }

            if (!string.IsNullOrWhiteSpace(vehicleType))
            {
                sql.Append(" AND pa.vehicle_types LIKE @p").Append(args.Count);
                args.Add("%" + vehicleType.Trim() + "%");
            }

            if (string.Equals(onlyAvailable, "true", StringComparison.OrdinalIgnoreCase))
            {
                sql.Append(" AND (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') > 0");
            }

            if (sort == "price_asc") sql.Append(" ORDER BY pa.price_per_hour ASC");
            else if (sort == "price_desc") sql.Append(" ORDER BY pa.price_per_hour DESC");
            else if (sort == "availability")
            {
                sql.Append(" ORDER BY (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') DESC");
            }
            else sql.Append(" ORDER BY pa.created_at DESC");

            return _db.Query(sql.ToString(), Maps.Area, args.ToArray());
        }

        public AreaDetailResponse GetById(int id)
        {
            var area = _db.QuerySingle(Maps.AreaWithAvailabilitySql + " WHERE pa.id = @p0", Maps.Area, id);
            if (area == null) throw ApiException.NotFound("Parking area not found.");

            var slots = _db.Query(
                "SELECT * FROM slots WHERE parking_area_id = @p0 ORDER BY slot_number",
                Maps.Slot,
                id);

            var reviews = _db.Query(
                @"SELECT r.id, r.rating, r.comment, r.created_at, u.name AS user_name
                  FROM reviews r JOIN users u ON u.id = r.user_id
                  WHERE r.parking_area_id = @p0 ORDER BY r.created_at DESC",
                Maps.Review,
                id);

            double? avg = null;
            if (reviews.Count > 0)
            {
                var sum = 0;
                foreach (var review in reviews) sum += review.Rating;
                avg = Math.Round(sum / (double)reviews.Count, 1);
            }

            return new AreaDetailResponse
            {
                Area = area,
                Slots = slots,
                Reviews = reviews,
                AvgRating = avg
            };
        }

        public List<ParkingAreaDto> Mine(int ownerId, bool isAdmin)
        {
            if (isAdmin)
            {
                return _db.Query(Maps.AreaWithAvailabilitySql + " ORDER BY pa.created_at DESC", Maps.Area);
            }

            return _db.Query(Maps.AreaWithAvailabilitySql + " WHERE pa.owner_id = @p0 ORDER BY pa.created_at DESC", Maps.Area, ownerId);
        }

        public ParkingAreaDto Create(int ownerId, CreateParkingAreaRequest request)
        {
            if (request == null ||
                string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Address) ||
                string.IsNullOrWhiteSpace(request.City) ||
                !request.PricePerHour.HasValue ||
                request.PricePerHour.Value < 0)
            {
                throw ApiException.BadRequest("Please fill all required fields correctly.");
            }

            return _db.Write((connection, transaction) =>
            {
                ParkingDb.Exec(
                    connection,
                    transaction,
                    @"INSERT INTO parking_areas
                      (owner_id, name, address, city, latitude, longitude, description, price_per_hour, opening_time, closing_time, vehicle_types)
                      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)",
                    ownerId,
                    request.Name.Trim(),
                    request.Address.Trim(),
                    request.City.Trim(),
                    request.Latitude,
                    request.Longitude,
                    string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                    request.PricePerHour.Value,
                    string.IsNullOrWhiteSpace(request.OpeningTime) ? "06:00" : request.OpeningTime,
                    string.IsNullOrWhiteSpace(request.ClosingTime) ? "22:00" : request.ClosingTime,
                    string.IsNullOrWhiteSpace(request.VehicleTypes) ? "car,bike" : request.VehicleTypes);

                var id = ParkingDb.LastInsertId(connection, transaction);
                return ParkingDb.QueryOne(connection, transaction, Maps.AreaWithAvailabilitySql + " WHERE pa.id = @p0", Maps.Area, id);
            });
        }

        public ParkingAreaDto Update(int id, int userId, string role, CreateParkingAreaRequest request)
        {
            AssertOwnsArea(id, userId, role);

            var sets = new List<string>();
            var args = new List<object>();
            void Set(string column, object value)
            {
                sets.Add(column + " = @p" + args.Count);
                args.Add(value);
            }

            if (request.Name != null) Set("name", request.Name);
            if (request.Address != null) Set("address", request.Address);
            if (request.City != null) Set("city", request.City);
            if (request.Latitude.HasValue) Set("latitude", request.Latitude.Value);
            if (request.Longitude.HasValue) Set("longitude", request.Longitude.Value);
            if (request.Description != null) Set("description", request.Description);
            if (request.PricePerHour.HasValue) Set("price_per_hour", request.PricePerHour.Value);
            if (request.OpeningTime != null) Set("opening_time", request.OpeningTime);
            if (request.ClosingTime != null) Set("closing_time", request.ClosingTime);
            if (request.VehicleTypes != null) Set("vehicle_types", request.VehicleTypes);

            if (sets.Count == 0) throw ApiException.BadRequest("No fields to update.");

            var sql = "UPDATE parking_areas SET " + string.Join(", ", sets) + " WHERE id = @p" + args.Count;
            args.Add(id);
            _db.Execute(sql, args.ToArray());
            return _db.QuerySingle(Maps.AreaWithAvailabilitySql + " WHERE pa.id = @p0", Maps.Area, id);
        }

        public void Delete(int id, int userId, string role)
        {
            AssertOwnsArea(id, userId, role);
            var active = _db.Scalar(
                @"SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
                  WHERE s.parking_area_id = @p0 AND b.status = 'confirmed' LIMIT 1",
                id);
            if (active != null && active != DBNull.Value)
            {
                throw ApiException.Conflict("Cannot delete a parking area with active bookings.");
            }

            _db.Execute("DELETE FROM parking_areas WHERE id = @p0", id);
        }

        public void AssertOwnsArea(int areaId, int userId, string role)
        {
            var ownerIdObj = _db.Scalar("SELECT owner_id FROM parking_areas WHERE id = @p0", areaId);
            if (ownerIdObj == null || ownerIdObj == DBNull.Value)
            {
                throw ApiException.NotFound("Parking area not found.");
            }

            if (role != "admin" && Convert.ToInt32(ownerIdObj) != userId)
            {
                throw ApiException.Forbidden("You can only manage slots in your own parking areas.");
            }
        }
    }
}
