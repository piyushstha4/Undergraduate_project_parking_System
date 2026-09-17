const express = require('express');
const { body, validationResult } = require('express-validator');
const db = require('../db');
const { authenticate, authorize } = require('../middleware/auth');

// Two separate routers to avoid path collisions when mounted:
//   areaSlotsRouter -> mounted at /api/parking-areas  (POST /:areaId/slots)
//   slotRouter      -> mounted at /api/slots          (PUT|DELETE /:id)
const router = express.Router();
const areaSlotsRouter = express.Router();

function assertOwnsArea(req, areaId) {
  const area = db.prepare('SELECT * FROM parking_areas WHERE id = ?').get(areaId);
  if (!area) return { error: 404, message: 'Parking area not found.' };
  if (req.user.role !== 'admin' && area.owner_id !== req.user.id) {
    return { error: 403, message: 'You can only manage slots in your own parking areas.' };
  }
  return { area };
}

// POST /api/parking-areas/:areaId/slots — add a slot
areaSlotsRouter.post(
  '/:areaId/slots',
  authenticate,
  authorize('parking_owner', 'admin'),
  [body('slot_number').trim().notEmpty()],
  (req, res) => {
    const errors = validationResult(req);
    if (!errors.isEmpty()) return res.status(400).json({ error: 'Slot number is required.' });

    const check = assertOwnsArea(req, req.params.areaId);
    if (check.error) return res.status(check.error).json({ error: check.message });

    const { slot_number, vehicle_type } = req.body;
    try {
      const info = db
        .prepare('INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (?, ?, ?)')
        .run(req.params.areaId, slot_number, vehicle_type === 'bike' ? 'bike' : 'car');
      const slot = db.prepare('SELECT * FROM slots WHERE id = ?').get(info.lastInsertRowid);
      res.status(201).json({ slot });
    } catch (e) {
      if (String(e.message).includes('UNIQUE')) {
        return res.status(409).json({ error: 'A slot with this number already exists in this area.' });
      }
      throw e;
    }
  }
);

// PUT /api/slots/:id — update a slot (status, vehicle_type, slot_number)
router.put('/:id', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const slot = db.prepare('SELECT * FROM slots WHERE id = ?').get(req.params.id);
  if (!slot) return res.status(404).json({ error: 'Slot not found.' });
  const check = assertOwnsArea(req, slot.parking_area_id);
  if (check.error) return res.status(check.error).json({ error: check.message });

  const activeBooking = db
    .prepare("SELECT id FROM bookings WHERE slot_id = ? AND status = 'confirmed'")
    .get(slot.id);
  if (activeBooking && req.body.status && req.body.status !== 'booked') {
    return res.status(409).json({ error: 'Cannot change status of a slot with an active booking.' });
  }

  const fields = ['slot_number', 'vehicle_type', 'status'];
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
  db.prepare(`UPDATE slots SET ${updates.join(', ')} WHERE id = ?`).run(...params);
  res.json({ slot: db.prepare('SELECT * FROM slots WHERE id = ?').get(req.params.id) });
});

// DELETE /api/slots/:id
router.delete('/:id', authenticate, authorize('parking_owner', 'admin'), (req, res) => {
  const slot = db.prepare('SELECT * FROM slots WHERE id = ?').get(req.params.id);
  if (!slot) return res.status(404).json({ error: 'Slot not found.' });
  const check = assertOwnsArea(req, slot.parking_area_id);
  if (check.error) return res.status(check.error).json({ error: check.message });

  const activeBooking = db
    .prepare("SELECT id FROM bookings WHERE slot_id = ? AND status = 'confirmed'")
    .get(slot.id);
  if (activeBooking) {
    return res.status(409).json({ error: 'Cannot delete a slot with an active booking.' });
  }
  db.prepare('DELETE FROM slots WHERE id = ?').run(req.params.id);
  res.json({ success: true });
});

module.exports = router;
module.exports.areaSlotsRouter = areaSlotsRouter;
