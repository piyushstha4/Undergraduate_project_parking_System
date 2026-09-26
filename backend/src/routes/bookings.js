const express = require('express');
const crypto = require('crypto');
const { body, validationResult } = require('express-validator');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

const router = express.Router();

function bookingWithDetails(row) {
  return row;
}

const BOOKING_DETAIL_SQL = `
  SELECT b.*, s.slot_number, s.vehicle_type, pa.name AS area_name, pa.address AS area_address,
         pa.city AS area_city, pa.id AS area_id, u.name AS user_name, u.email AS user_email
  FROM bookings b
  JOIN slots s ON s.id = b.slot_id
  JOIN parking_areas pa ON pa.id = s.parking_area_id
  JOIN users u ON u.id = b.user_id
`;

// Release slots whose confirmed booking has ended (auto-release mechanism, Objective #3)
function releaseExpiredBookings() {
  const now = new Date().toISOString();
  const expired = db
    .prepare("SELECT * FROM bookings WHERE status = 'confirmed' AND end_time <= ?")
    .all(now);
  const markCompleted = db.prepare("UPDATE bookings SET status = 'completed' WHERE id = ?");
  const freeSlot = db.prepare("UPDATE slots SET status = 'available' WHERE id = ?");
  const tx = db.transaction((rows) => {
    for (const b of rows) {
      markCompleted.run(b.id);
      freeSlot.run(b.slot_id);
    }
  });
  if (expired.length) tx(expired);
  return expired.length;
}

// Run on every request to this router so state is always fresh (demo-friendly; a real
// deployment would run this on a timer/cron instead).
router.use((req, res, next) => {
  releaseExpiredBookings();
  next();
});

// POST /api/bookings — Slot Booking & Reservation Module + simulated Payment Module
router.post(
  '/',
  authenticate,
  authorize('user', 'admin'),
  [
    body('slot_id').isInt(),
    body('start_time').isISO8601(),
    body('end_time').isISO8601(),
    body('payment_method').optional().isIn(['esewa', 'khalti', 'card']),
  ],
  (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ error: 'Please provide a valid slot and time range.' });

    const { slot_id, start_time, end_time, vehicle_plate, payment_method } = req.body;
    const start = new Date(start_time);
    const end = new Date(end_time);

    if (!(start < end)) return res.status(400).json({ error: 'End time must be after start time.' });
    if (start < new Date(Date.now() - 5 * 60 * 1000)) {
      return res.status(400).json({ error: 'Start time cannot be in the past.' });
    }

    const slot = db.prepare('SELECT * FROM slots WHERE id = ?').get(slot_id);
    if (!slot) return res.status(404).json({ error: 'Slot not found.' });
    if (slot.status === 'maintenance') {
      return res.status(409).json({ error: 'This slot is currently under maintenance.' });
    }

    const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(slot.parking_area_id);

    try {
      const result = db.transaction(() => {
        // Overlap check guards against booking-concurrency race conditions (flagged in
        // Chapter 8 Critical Analysis) — two users cannot reserve the same slot for
        // overlapping windows. better-sqlite3 runs synchronously so this whole
        // transaction is serialized against any other write.
        const overlap = db
          .prepare(
            `SELECT id FROM bookings
             WHERE slot_id = ? AND status = 'confirmed'
               AND NOT (end_time <= ? OR start_time >= ?)`
          )
          .get(slot_id, start_time, end_time);
        if (overlap) {
          const err = new Error('This slot is already booked for part of the selected time range.');
          err.code = 'SLOT_TAKEN';
          throw err;
        }

        const hours = Math.max(1, Math.ceil((end - start) / (1000 * 60 * 60)));
        const total_amount = Math.round(hours * area.price_per_hour * 100) / 100;

        const bookingInfo = db
          .prepare(
            `INSERT INTO bookings (user_id, slot_id, start_time, end_time, status, total_amount, vehicle_plate)
             VALUES (?, ?, ?, ?, 'pending_payment', ?, ?)`
          )
          .run(req.user.id, slot_id, start_time, end_time, total_amount, vehicle_plate || null);

        const bookingId = bookingInfo.lastInsertRowid;

        // Simulated payment gateway (Payment & Transaction Module) — always succeeds in
        // this demo build. A real integration would call out to eSewa/Khalti/Stripe here
        // and only confirm on a verified callback.
        const txnRef = `TXN-${Date.now()}-${crypto.randomBytes(3).toString('hex').toUpperCase()}`;
        db.prepare(
          `INSERT INTO payments (booking_id, amount, method, status, transaction_ref)
           VALUES (?, ?, ?, 'success', ?)`
        ).run(bookingId, total_amount, payment_method || 'card', txnRef);

        db.prepare("UPDATE bookings SET status = 'confirmed' WHERE id = ?").run(bookingId);
        db.prepare("UPDATE slots SET status = 'booked' WHERE id = ?").run(slot_id);

        return bookingId;
      })();

      const booking = db.prepare(`${BOOKING_DETAIL_SQL} WHERE b.id = ?`).get(result);
      res.status(201).json({ booking: bookingWithDetails(booking) });
    } catch (e) {
      if (e.code === 'SLOT_TAKEN') return res.status(409).json({ error: e.message });
      throw e;
    }
  }
);

// GET /api/bookings/me — booking history for the logged-in user
router.get('/me', authenticate, (req, res) => {
  const rows = db.prepare(`${BOOKING_DETAIL_SQL} WHERE b.user_id = ? ORDER BY b.created_at DESC`).all(req.user.id);
  res.json({ bookings: rows });
});

// PUT /api/bookings/:id/cancel — cancellation option (Objective #3)
router.put('/:id/cancel', authenticate, (req, res) => {
  const booking = db.prepare('SELECT * FROM bookings WHERE id = ?').get(req.params.id);
  if (!booking) return res.status(404).json({ error: 'Booking not found.' });

  const slot = db.prepare('SELECT * FROM slots WHERE id = ?').get(booking.slot_id);
  const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(slot.parking_area_id);
  const isOwnerOfArea = area && area.owner_id === req.user.id;

  if (req.user.role !== 'admin' && booking.user_id !== req.user.id && !isOwnerOfArea) {
    return res.status(403).json({ error: 'You can only cancel your own bookings.' });
  }
  if (!['confirmed', 'pending_payment'].includes(booking.status)) {
    return res.status(409).json({ error: `Booking is already ${booking.status} and cannot be cancelled.` });
  }

  db.transaction(() => {
    db.prepare("UPDATE bookings SET status = 'cancelled' WHERE id = ?").run(booking.id);
    db.prepare("UPDATE slots SET status = 'available' WHERE id = ?").run(booking.slot_id);
  })();

  const updated = db.prepare(`${BOOKING_DETAIL_SQL} WHERE b.id = ?`).get(booking.id);
  res.json({ booking: updated });
});

// GET /api/bookings — owner/admin view, filterable by area
router.get('/', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const { areaId, status } = req.query;
  let sql = BOOKING_DETAIL_SQL + ' WHERE 1=1';
  const params = [];

  if (req.user.role !== 'admin') {
    sql += ' AND pa.owner_id = ?';
    params.push(req.user.id);
  }
  if (areaId) {
    sql += ' AND pa.id = ?';
    params.push(areaId);
  }
  if (status) {
    sql += ' AND b.status = ?';
    params.push(status);
  }
  sql += ' ORDER BY b.created_at DESC';

  res.json({ bookings: db.prepare(sql).all(...params) });
});

module.exports = router;
module.exports.releaseExpiredBookings = releaseExpiredBookings;
