const express = require('express');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

const router = express.Router();
router.use(authenticate, authorize('admin'));

// GET /api/admin/stats — system-wide dashboard overview
router.get('/stats', (req, res) => {
  const totalUsers = db.prepare("SELECT COUNT(*) AS c FROM users WHERE role = 'user'").get().c;
  const totalOwners = db.prepare("SELECT COUNT(*) AS c FROM users WHERE role = 'parking_owner'").get().c;
  const totalAreas = db.prepare('SELECT COUNT(*) AS c FROM parking_areas').get().c;
  const totalSlots = db.prepare('SELECT COUNT(*) AS c FROM slots').get().c;
  const availableSlots = db.prepare("SELECT COUNT(*) AS c FROM slots WHERE status = 'available'").get().c;
  const activeBookings = db.prepare("SELECT COUNT(*) AS c FROM bookings WHERE status = 'confirmed'").get().c;
  const totalBookings = db.prepare('SELECT COUNT(*) AS c FROM bookings').get().c;
  const revenue = db.prepare("SELECT COALESCE(SUM(amount),0) AS s FROM payments WHERE status = 'success'").get().s;
  const bookingsByStatus = db
    .prepare('SELECT status, COUNT(*) AS count FROM bookings GROUP BY status')
    .all();
  const recentBookings = db
    .prepare(
      `SELECT b.id, b.status, b.total_amount, b.created_at, u.name AS user_name, pa.name AS area_name
       FROM bookings b JOIN users u ON u.id = b.user_id
       JOIN slots s ON s.id = b.slot_id JOIN parking_areas pa ON pa.id = s.parking_area_id
       ORDER BY b.created_at DESC LIMIT 8`
    )
    .all();

  res.json({
    totalUsers, totalOwners, totalAreas, totalSlots, availableSlots,
    activeBookings, totalBookings, revenue, bookingsByStatus, recentBookings,
  });
});

// GET /api/admin/users — manage users & parking owners
router.get('/users', (req, res) => {
  const { role } = req.query;
  let sql = "SELECT id, name, email, phone, role, status, created_at FROM users WHERE role != 'admin'";
  const params = [];
  if (role) {
    sql += ' AND role = ?';
    params.push(role);
  }
  sql += ' ORDER BY created_at DESC';
  res.json({ users: db.prepare(sql).all(...params) });
});

// PUT /api/admin/users/:id/status — suspend / reactivate a user or owner
router.put('/users/:id/status', (req, res) => {
  const { status } = req.body;
  if (!['active', 'suspended'].includes(status)) {
    return res.status(400).json({ error: 'Status must be "active" or "suspended".' });
  }
  const user = db.prepare('SELECT * FROM users WHERE id = ?').get(req.params.id);
  if (!user) return res.status(404).json({ error: 'User not found.' });
  if (user.role === 'admin') return res.status(403).json({ error: 'Cannot modify an admin account.' });

  db.prepare('UPDATE users SET status = ? WHERE id = ?').run(status, req.params.id);
  const updated = db.prepare('SELECT id, name, email, phone, role, status, created_at FROM users WHERE id = ?').get(req.params.id);
  res.json({ user: updated });
});

// GET /api/admin/parking-areas — all facilities system-wide, with owner info
router.get('/parking-areas', (req, res) => {
  const areas = db
    .prepare(
      `SELECT pa.*, u.name AS owner_name, u.email AS owner_email,
        (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id) AS total_slots,
        (SELECT COUNT(*) FROM slots s WHERE s.parking_area_id = pa.id AND s.status = 'available') AS available_slots
       FROM parking_areas pa JOIN users u ON u.id = pa.owner_id
       ORDER BY pa.created_at DESC`
    )
    .all();
  res.json({ areas });
});

module.exports = router;
