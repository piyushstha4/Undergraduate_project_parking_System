const express = require('express');
const { body, validationResult } = require('express-validator');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

const router = express.Router();

function availableCount(areaId) {
  const row = db
    .prepare("SELECT COUNT(*) AS c FROM slots WHERE parking_area_id = ? AND status = 'available'")
    .get(areaId);
  return row.c;
}

function totalCount(areaId) {
  const row = db.prepare('SELECT COUNT(*) AS c FROM slots WHERE parking_area_id = ?').get(areaId);
  return row.c;
}

function withAvailability(area) {
  return { ...area, available_slots: availableCount(area.id), total_slots: totalCount(area.id) };
}

// GET /api/parking-areas  — Search & Filtering Module
// query params: q (name/address/city), city, maxPrice, vehicleType, onlyAvailable
router.get('/', (req, res) => {
  const { q, city, maxPrice, vehicleType, onlyAvailable, sort } = req.query;
  let sql = 'SELECT * FROM parking_areas WHERE 1=1';
  const params = [];

  if (q) {
    sql += ' AND (name LIKE ? OR address LIKE ? OR city LIKE ?)';
    const like = `%${q}%`;
    params.push(like, like, like);
  }
  if (city) {
    sql += ' AND city LIKE ?';
    params.push(`%${city}%`);
  }
  if (maxPrice) {
    sql += ' AND price_per_hour <= ?';
    params.push(Number(maxPrice));
  }
  if (vehicleType) {
    sql += ' AND vehicle_types LIKE ?';
    params.push(`%${vehicleType}%`);
  }

  let areas = db.prepare(sql).all(...params).map(withAvailability);

  if (onlyAvailable === 'true') {
    areas = areas.filter((a) => a.available_slots > 0);
  }

  if (sort === 'price_asc') areas.sort((a, b) => a.price_per_hour - b.price_per_hour);
  else if (sort === 'price_desc') areas.sort((a, b) => b.price_per_hour - a.price_per_hour);
  else if (sort === 'availability') areas.sort((a, b) => b.available_slots - a.available_slots);

  res.json({ areas });
});

// GET /api/parking-areas/:id
router.get('/:id', (req, res) => {
  const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(req.params.id);
  if (!area) return res.status(404).json({ error: 'Parking area not found.' });
  const slots = db.prepare('SELECT * FROM slots WHERE parking_area_id = ? ORDER BY slot_number').all(area.id);
  const reviews = db
    .prepare(
      `SELECT r.id, r.rating, r.comment, r.created_at, u.name AS user_name
       FROM reviews r JOIN users u ON u.id = r.user_id
       WHERE r.parking_area_id = ? ORDER BY r.created_at DESC`
    )
    .all(area.id);
  const avgRating = reviews.length
    ? Math.round((reviews.reduce((s, r) => s + r.rating, 0) / reviews.length) * 10) / 10
    : null;

  res.json({ area: withAvailability(area), slots, reviews, avgRating });
});

// POST /api/parking-areas  — owner creates a new facility
router.post(
  '/',
  authenticate,
  authorize('parking_owner', 'admin'),
  [
    body('name').trim().notEmpty(),
    body('address').trim().notEmpty(),
    body('city').trim().notEmpty(),
    body('price_per_hour').isFloat({ min: 0 }),
  ],
  (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ error: 'Please fill all required fields correctly.' });

    const {
      name, address, city, latitude, longitude, description,
      price_per_hour, opening_time, closing_time, vehicle_types,
    } = req.body;

    const info = db
      .prepare(
        `INSERT INTO parking_areas
         (owner_id, name, address, city, latitude, longitude, description, price_per_hour, opening_time, closing_time, vehicle_types)
         VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)`
      )
      .run(
        req.user.id, name, address, city, latitude || null, longitude || null,
        description || null, price_per_hour, opening_time || '06:00', closing_time || '22:00',
        vehicle_types || 'car,bike'
      );

    const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(info.lastInsertRowid);
    res.status(201).json({ area: withAvailability(area) });
  }
);

// PUT /api/parking-areas/:id  — owner updates their facility
router.put('/:id', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(req.params.id);
  if (!area) return res.status(404).json({ error: 'Parking area not found.' });
  if (req.user.role !== 'admin' && area.owner_id !== req.user.id) {
    return res.status(403).json({ error: 'You can only edit your own parking areas.' });
  }

  const fields = ['name', 'address', 'city', 'latitude', 'longitude', 'description', 'price_per_hour', 'opening_time', 'closing_time', 'vehicle_types'];
  const updates = [];
  const params = [];
  for (const f of fields) {
    if (req.body[f] !== undefined) {
      updates.push(`${f} = ?`);
      params.push(req.body[f]);
    }
  }
  if (updates.length === 0) return res.status(400).json({ error: 'No fields to update.' });
  params.push(req.params.id);
  db.prepare(`UPDATE parking_areas SET ${updates.join(', ')} WHERE id = ?`).run(...params);

  const updated = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(req.params.id);
  res.json({ area: withAvailability(updated) });
});

// DELETE /api/parking-areas/:id
router.delete('/:id', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(req.params.id);
  if (!area) return res.status(404).json({ error: 'Parking area not found.' });
  if (req.user.role !== 'admin' && area.owner_id !== req.user.id) {
    return res.status(403).json({ error: 'You can only delete your own parking areas.' });
  }
  const activeBooking = db
    .prepare(
      `SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
       WHERE s.parking_area_id = ? AND b.status = 'confirmed' LIMIT 1`
    )
    .get(req.params.id);
  if (activeBooking) {
    return res.status(409).json({ error: 'Cannot delete a parking area with active bookings.' });
  }
  db.prepare('DELETE FROM parking_areas WHERE id = ?').run(req.params.id);
  res.json({ success: true });
});

// GET /api/parking-areas/owner/mine — owner's own facilities
router.get('/owner/mine', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const areas = db.prepare('SELECT * FROM parking_areas WHERE owner_id = ?').all(req.user.id).map(withAvailability);
  res.json({ areas });
});

module.exports = router;
