const express = require('express');
const { body, validationResult } = require('express-validator');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

const router = express.Router();

// POST /api/parking-areas/:areaId/reviews
router.post(
  '/:areaId/reviews',
  authenticate,
  authorize('user', 'admin'),
  [body('rating').isInt({ min: 1, max: 5 })],
  (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ error: 'Rating must be between 1 and 5.' });

    const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(req.params.areaId);
    if (!area) return res.status(404).json({ error: 'Parking area not found.' });

    const hasBooking = db
      .prepare(
        `SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
         WHERE s.parking_area_id = ? AND b.user_id = ? AND b.status IN ('confirmed','completed') LIMIT 1`
      )
      .get(req.params.areaId, req.user.id);
    if (!hasBooking) {
      return res.status(403).json({ error: 'You can only review parking areas you have booked.' });
    }

    const { rating, comment } = req.body;
    db.prepare('INSERT INTO reviews (user_id, parking_area_id, rating, comment) VALUES (?, ?, ?, ?)').run(
      req.user.id, req.params.areaId, rating, comment || null
    );
    res.status(201).json({ success: true });
  }
);

module.exports = router;
