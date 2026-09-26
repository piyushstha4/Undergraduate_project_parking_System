require('dotenv').config();
const express = require('express');
const cors = require('cors');

const authRoutes = require('./routes/auth');
const parkingAreaRoutes = require('./routes/parkingAreas');
const slotRoutes = require('./routes/slots');
const bookingRoutes = require('./routes/bookings');
const adminRoutes = require('./routes/admin');
const ownerRoutes = require('./routes/owner');
const reviewRoutes = require('./routes/reviews');

const app = express();
app.use(cors());
app.use(express.json());

app.get('/api/health', (req, res) => res.json({ status: 'ok', service: 'smart-parking-api' }));

app.use('/api/auth', authRoutes);
app.use('/api/parking-areas', parkingAreaRoutes);
app.use('/api/parking-areas', reviewRoutes); // adds POST /:areaId/reviews
app.use('/api/parking-areas', slotRoutes.areaSlotsRouter); // adds POST /:areaId/slots
app.use('/api/slots', slotRoutes); // handles PUT/DELETE /:id
app.use('/api/bookings', bookingRoutes);
app.use('/api/admin', adminRoutes);
app.use('/api/owner', ownerRoutes);

// Centralized error handler so unexpected errors never leak stack traces to clients
app.use((err, req, res, next) => {
  console.error(err);
  res.status(500).json({ error: 'Something went wrong on the server. Please try again.' });
});

app.use((req, res) => res.status(404).json({ error: 'Not found.' }));

const PORT = process.env.PORT || 4000;
app.listen(PORT, () => {
  console.log(`Smart Parking API listening on http://localhost:${PORT}`);
});
