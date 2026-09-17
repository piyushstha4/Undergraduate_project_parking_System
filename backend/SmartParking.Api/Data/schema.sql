-- Smart Parking Slot Booking and Management System
-- SQLite schema mirroring the ERD in the Contextual Report (Chapter 5.4.2)
-- users -> parking_areas (ownership) -> slots -> bookings -> payments
--                                                 users -> reviews -> parking_areas

PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS users (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  name TEXT NOT NULL,
  email TEXT NOT NULL UNIQUE,
  phone TEXT,
  password_hash TEXT NOT NULL,
  role TEXT NOT NULL CHECK (role IN ('user', 'parking_owner', 'admin')) DEFAULT 'user',
  status TEXT NOT NULL CHECK (status IN ('active', 'suspended')) DEFAULT 'active',
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS parking_areas (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  owner_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  name TEXT NOT NULL,
  address TEXT NOT NULL,
  city TEXT NOT NULL,
  latitude REAL,
  longitude REAL,
  description TEXT,
  price_per_hour REAL NOT NULL,
  opening_time TEXT NOT NULL DEFAULT '06:00',
  closing_time TEXT NOT NULL DEFAULT '22:00',
  vehicle_types TEXT NOT NULL DEFAULT 'car,bike',
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS slots (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  parking_area_id INTEGER NOT NULL REFERENCES parking_areas(id) ON DELETE CASCADE,
  slot_number TEXT NOT NULL,
  vehicle_type TEXT NOT NULL CHECK (vehicle_type IN ('car', 'bike')) DEFAULT 'car',
  status TEXT NOT NULL CHECK (status IN ('available', 'booked', 'occupied', 'maintenance')) DEFAULT 'available',
  created_at TEXT NOT NULL DEFAULT (datetime('now')),
  UNIQUE (parking_area_id, slot_number)
);

CREATE TABLE IF NOT EXISTS bookings (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  slot_id INTEGER NOT NULL REFERENCES slots(id) ON DELETE CASCADE,
  start_time TEXT NOT NULL,
  end_time TEXT NOT NULL,
  status TEXT NOT NULL CHECK (status IN ('pending_payment', 'confirmed', 'cancelled', 'completed', 'expired')) DEFAULT 'pending_payment',
  total_amount REAL NOT NULL,
  vehicle_plate TEXT,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS payments (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  booking_id INTEGER NOT NULL REFERENCES bookings(id) ON DELETE CASCADE,
  amount REAL NOT NULL,
  method TEXT NOT NULL CHECK (method IN ('esewa', 'khalti', 'card')) DEFAULT 'card',
  status TEXT NOT NULL CHECK (status IN ('success', 'failed')) DEFAULT 'success',
  transaction_ref TEXT NOT NULL UNIQUE,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE TABLE IF NOT EXISTS reviews (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  user_id INTEGER NOT NULL REFERENCES users(id) ON DELETE CASCADE,
  parking_area_id INTEGER NOT NULL REFERENCES parking_areas(id) ON DELETE CASCADE,
  rating INTEGER NOT NULL CHECK (rating BETWEEN 1 AND 5),
  comment TEXT,
  created_at TEXT NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX IF NOT EXISTS idx_slots_area ON slots(parking_area_id);
CREATE INDEX IF NOT EXISTS idx_bookings_slot ON bookings(slot_id);
CREATE INDEX IF NOT EXISTS idx_bookings_user ON bookings(user_id);
CREATE INDEX IF NOT EXISTS idx_bookings_status ON bookings(status);
CREATE INDEX IF NOT EXISTS idx_areas_owner ON parking_areas(owner_id);
