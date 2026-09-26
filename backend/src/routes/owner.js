const express = require('express');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

const router = express.Router();
router.use(authenticate, authorize('parking_owner', 'admin'));

// GET /api/owner/reports — usage report across the owner's parking areas
// (Admin Dashboard & Monitoring Module: "Generate basic reports")
router.get('/reports', (req, res) => {
  const ownerFilter = req.user.role === 'admin' ? '' : 'WHERE pa.owner_id = ?';
  const params = req.user.role === 'admin' ? [] : [req.user.id];

  const areas = db
    .prepare(
      `SELECT pa.id, pa.name, pa.city, pa.price_per_hour,
        (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
        (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots,
        (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id WHERE s.parking_area_id = pa.id) AS total_bookings,
        (SELECT COUNT(*) FROM bookings b JOIN slots s ON s.id = b.slot_id WHERE s.parking_area_id = pa.id AND b.status = 'confirmed') AS active_bookings,
        (SELECT COALESCE(SUM(b.total_amount),0) FROM bookings b JOIN slots s ON s.id = b.slot_id
           WHERE s.parking_area_id = pa.id AND b.status IN ('confirmed','completed')) AS revenue
       FROM parking_areas pa ${ownerFilter}
       ORDER BY revenue DESC`
    )
    .all(...params);

  const totals = areas.reduce(
    (acc, a) => {
      acc.totalBookings += a.total_bookings;
      acc.activeBookings += a.active_bookings;
      acc.revenue += a.revenue;
      acc.totalSlots += a.total_slots;
      acc.availableSlots += a.available_slots;
      return acc;
    },
    { totalBookings: 0, activeBookings: 0, revenue: 0, totalSlots: 0, availableSlots: 0 }
  );

  res.json({ areas, totals });
});

module.exports = router;
