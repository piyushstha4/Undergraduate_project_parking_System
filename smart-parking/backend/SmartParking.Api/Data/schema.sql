-- Smart Parking Slot Booking and Management System
-- MySQL 8 / MariaDB 10.4+ schema (ported from the original SQLite schema).
-- Mirrors the ERD in the Contextual Report (Chapter 5.4.2):
--   users -> parking_areas (ownership) -> slots -> bookings -> payments
--   users -> reviews -> parking_areas
--
-- All DATETIME values are stored in UTC. The API sets the session time zone
-- to UTC ('+00:00') on every connection, so CURRENT_TIMESTAMP is UTC too.
-- Safe to run more than once (CREATE TABLE IF NOT EXISTS).

CREATE TABLE IF NOT EXISTS users (
  id            INT AUTO_INCREMENT PRIMARY KEY,
  name          VARCHAR(100)  NOT NULL,
  email         VARCHAR(255)  NOT NULL,
  phone         VARCHAR(20)   NULL,
  password_hash VARCHAR(255)  NOT NULL,
  role          ENUM('user','parking_owner','admin') NOT NULL DEFAULT 'user',
  status        ENUM('active','suspended')           NOT NULL DEFAULT 'active',
  created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT uq_users_email UNIQUE (email)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS parking_areas (
  id             INT AUTO_INCREMENT PRIMARY KEY,
  owner_id       INT           NOT NULL,
  name           VARCHAR(150)  NOT NULL,
  address        VARCHAR(255)  NOT NULL,
  city           VARCHAR(100)  NOT NULL,
  latitude       DOUBLE        NULL,
  longitude      DOUBLE        NULL,
  description    TEXT          NULL,
  price_per_hour DECIMAL(10,2) NOT NULL,
  capacity       INT           NOT NULL DEFAULT 1,
  opening_time   VARCHAR(5)    NOT NULL DEFAULT '06:00',
  closing_time   VARCHAR(5)    NOT NULL DEFAULT '22:00',
  vehicle_types  VARCHAR(20)   NOT NULL DEFAULT 'car,bike',
  created_at     DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_areas_owner (owner_id),
  CONSTRAINT fk_areas_owner FOREIGN KEY (owner_id) REFERENCES users(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS slots (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  parking_area_id INT          NOT NULL,
  slot_number     VARCHAR(20)  NOT NULL,
  vehicle_type    ENUM('car','bike') NOT NULL DEFAULT 'car',
  status          ENUM('available','booked','occupied','maintenance') NOT NULL DEFAULT 'available',
  created_at      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT uq_slots_area_number UNIQUE (parking_area_id, slot_number),
  INDEX idx_slots_area (parking_area_id),
  CONSTRAINT fk_slots_area FOREIGN KEY (parking_area_id) REFERENCES parking_areas(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS bookings (
  id            INT AUTO_INCREMENT PRIMARY KEY,
  user_id       INT           NOT NULL,
  slot_id       INT           NOT NULL,
  start_time    DATETIME      NOT NULL,
  end_time      DATETIME      NOT NULL,
  status        ENUM('pending_payment','confirmed','cancelled','completed','expired') NOT NULL DEFAULT 'pending_payment',
  total_amount  DECIMAL(10,2) NOT NULL,
  vehicle_plate VARCHAR(20)   NULL,
  created_at    DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  INDEX idx_bookings_slot (slot_id),
  INDEX idx_bookings_user (user_id),
  INDEX idx_bookings_status (status),
  CONSTRAINT fk_bookings_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  CONSTRAINT fk_bookings_slot FOREIGN KEY (slot_id) REFERENCES slots(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS payments (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  booking_id      INT           NOT NULL,
  amount          DECIMAL(10,2) NOT NULL,
  method          ENUM('esewa','khalti','card') NOT NULL DEFAULT 'card',
  status          ENUM('success','failed')      NOT NULL DEFAULT 'success',
  transaction_ref VARCHAR(50)   NOT NULL,
  created_at      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT uq_payments_txn UNIQUE (transaction_ref),
  INDEX idx_payments_booking (booking_id),
  CONSTRAINT fk_payments_booking FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

CREATE TABLE IF NOT EXISTS reviews (
  id              INT AUTO_INCREMENT PRIMARY KEY,
  user_id         INT      NOT NULL,
  parking_area_id INT      NOT NULL,
  rating          TINYINT  NOT NULL,
  comment         TEXT     NULL,
  created_at      DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT chk_reviews_rating CHECK (rating BETWEEN 1 AND 5),
  INDEX idx_reviews_area (parking_area_id),
  CONSTRAINT fk_reviews_user FOREIGN KEY (user_id) REFERENCES users(id) ON DELETE CASCADE,
  CONSTRAINT fk_reviews_area FOREIGN KEY (parking_area_id) REFERENCES parking_areas(id) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
