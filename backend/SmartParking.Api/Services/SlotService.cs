using System;
using System.Collections.Generic;
using System.Data.SQLite;
using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class SlotService
    {
        private static readonly HashSet<string> AllowedStatuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "available", "booked", "occupied", "maintenance"
        };

        private readonly ParkingDb _db;
        private readonly ParkingAreaService _areas;

        public SlotService(ParkingDb db, ParkingAreaService areas)
        {
            _db = db;
            _areas = areas;
        }

        public SlotDto Create(int areaId, int userId, string role, CreateSlotRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.SlotNumber))
            {
                throw ApiException.BadRequest("Slot number is required.");
            }

            _areas.AssertOwnsArea(areaId, userId, role);
            var vehicleType = request.VehicleType == "bike" ? "bike" : "car";

            try
            {
                return _db.Write((connection, transaction) =>
                {
                    ParkingDb.Exec(
                        connection,
                        transaction,
                        "INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (@p0, @p1, @p2)",
                        areaId,
                        request.SlotNumber.Trim(),
                        vehicleType);
                    var id = ParkingDb.LastInsertId(connection, transaction);
                    return ParkingDb.QueryOne(connection, transaction, "SELECT * FROM slots WHERE id = @p0", Maps.Slot, id);
                });
            }
            catch (SQLiteException ex) when (ex.ResultCode == SQLiteErrorCode.Constraint)
            {
                throw ApiException.Conflict("A slot with this number already exists in this area.");
            }
        }

        public SlotDto Update(int slotId, int userId, string role, UpdateSlotRequest request)
        {
            var slot = _db.QuerySingle("SELECT * FROM slots WHERE id = @p0", Maps.Slot, slotId);
            if (slot == null) throw ApiException.NotFound("Slot not found.");
            _areas.AssertOwnsArea(slot.ParkingAreaId, userId, role);

            if (request == null) throw ApiException.BadRequest("No fields to update.");

            if (!string.IsNullOrEmpty(request.Status) && !AllowedStatuses.Contains(request.Status))
            {
                throw ApiException.BadRequest("Invalid slot status.");
            }

            if (!string.IsNullOrEmpty(request.VehicleType) && request.VehicleType != "car" && request.VehicleType != "bike")
            {
                throw ApiException.BadRequest("Vehicle type must be car or bike.");
            }

            var active = _db.Scalar("SELECT id FROM bookings WHERE slot_id = @p0 AND status = 'confirmed' LIMIT 1", slot.Id);
            if (active != null && active != DBNull.Value && !string.IsNullOrEmpty(request.Status) && request.Status != "booked")
            {
                throw ApiException.Conflict("Cannot change status of a slot with an active booking.");
            }

            var sets = new List<string>();
            var args = new List<object>();
            void Set(string column, object value)
            {
                sets.Add(column + " = @p" + args.Count);
                args.Add(value);
            }

            if (request.SlotNumber != null) Set("slot_number", request.SlotNumber);
            if (request.VehicleType != null) Set("vehicle_type", request.VehicleType);
            if (request.Status != null) Set("status", request.Status);
            if (sets.Count == 0) throw ApiException.BadRequest("No fields to update.");

            var sql = "UPDATE slots SET " + string.Join(", ", sets) + " WHERE id = @p" + args.Count;
            args.Add(slotId);
            _db.Execute(sql, args.ToArray());
            return _db.QuerySingle("SELECT * FROM slots WHERE id = @p0", Maps.Slot, slotId);
        }

        public void Delete(int slotId, int userId, string role)
        {
            var slot = _db.QuerySingle("SELECT * FROM slots WHERE id = @p0", Maps.Slot, slotId);
            if (slot == null) throw ApiException.NotFound("Slot not found.");
            _areas.AssertOwnsArea(slot.ParkingAreaId, userId, role);

            var active = _db.Scalar("SELECT id FROM bookings WHERE slot_id = @p0 AND status = 'confirmed' LIMIT 1", slot.Id);
            if (active != null && active != DBNull.Value)
            {
                throw ApiException.Conflict("Cannot delete a slot with an active booking.");
            }

            _db.Execute("DELETE FROM slots WHERE id = @p0", slotId);
        }
    }
}
