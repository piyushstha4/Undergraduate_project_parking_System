-- =====================================================================
-- Smart Parking Slot Booking and Management System - MySQL database
-- Works with MySQL 8.0+ and MariaDB 10.4+ (XAMPP / WampServer).
--
-- OPTIONAL: the ASP.NET API creates and seeds this database by itself on
-- first run. Use this script only if you want to set it up manually in
-- MySQL Workbench / phpMyAdmin (then set "CreateAndSeedOnStartup": false).
--
-- Demo accounts (password shown for grading/demo purposes only):
--   admin@smartparking.com  / Admin@123
--   owner1@smartparking.com / Owner@123
--   owner2@smartparking.com / Owner@123
--   user@smartparking.com   / User@123
-- =====================================================================

CREATE DATABASE IF NOT EXISTS smart_parking CHARACTER SET utf8mb4 COLLATE utf8mb4_general_ci;
USE smart_parking;
SET time_zone = '+00:00';

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

-- ---------------------------------------------------------------------
-- Demo data
-- ---------------------------------------------------------------------
INSERT IGNORE INTO `users` (`id`, `name`, `email`, `phone`, `password_hash`, `role`, `status`, `created_at`) VALUES (1,'System Admin','admin@smartparking.com','9800000000','$2a$10$vCtW3FlgmMreEJTFuvM4pu9BrDxISudWDMFOf8BaW9nF8AYIHAllG','admin','active','2026-09-24 02:27:56');
INSERT IGNORE INTO `users` (`id`, `name`, `email`, `phone`, `password_hash`, `role`, `status`, `created_at`) VALUES (2,'Ramesh Shrestha','owner1@smartparking.com','9801111111','$2a$10$DsTboxP7oZ0eMO3rcKUcDe.8nQGQOkQiRAc3z8E.69rkZtSOOzWG6','parking_owner','active','2026-09-24 02:27:56');
INSERT IGNORE INTO `users` (`id`, `name`, `email`, `phone`, `password_hash`, `role`, `status`, `created_at`) VALUES (3,'Sita Maharjan','owner2@smartparking.com','9802222222','$2a$10$tTOTtzTQqwFa6anWheQ5hu.jyBccPUY.nFkDX3wBJm6uv/YpWqExu','parking_owner','active','2026-09-24 02:27:56');
INSERT IGNORE INTO `users` (`id`, `name`, `email`, `phone`, `password_hash`, `role`, `status`, `created_at`) VALUES (4,'Piyush Shrestha','user@smartparking.com','9803333333','$2a$10$AdmTCC26o02ean0lGi7AeueYFekAkYWu2rVZLZI/X2Tj2e0tHG3DW','user','active','2026-09-24 02:27:56');
INSERT IGNORE INTO `users` (`id`, `name`, `email`, `phone`, `password_hash`, `role`, `status`, `created_at`) VALUES (5,'Anisha Gurung','anisha@smartparking.com','9804444444','$2a$10$YYnOUyaz6NuP05G0uMzIAutONsSYVVTtBFIR6f6SabMS8Sl5zPH5e','user','active','2026-09-24 02:27:56');
INSERT IGNORE INTO `parking_areas` (`id`, `owner_id`, `name`, `address`, `city`, `latitude`, `longitude`, `description`, `price_per_hour`, `opening_time`, `closing_time`, `vehicle_types`, `created_at`) VALUES (1,2,'City Center Mall Parking','Kamalpokhari, Kathmandu','Kathmandu',27.7089,85.3247,'Multi-level covered parking beside City Center Mall. CCTV monitored.',50.00,'07:00','22:00','car,bike','2026-09-24 02:27:56');
INSERT IGNORE INTO `parking_areas` (`id`, `owner_id`, `name`, `address`, `city`, `latitude`, `longitude`, `description`, `price_per_hour`, `opening_time`, `closing_time`, `vehicle_types`, `created_at`) VALUES (2,2,'New Road Business Parking','New Road, Kathmandu','Kathmandu',27.704,85.309,'Open-air parking close to New Road shopping street. High demand during peak hours.',40.00,'06:00','21:00','car,bike','2026-09-24 02:27:56');
INSERT IGNORE INTO `parking_areas` (`id`, `owner_id`, `name`, `address`, `city`, `latitude`, `longitude`, `description`, `price_per_hour`, `opening_time`, `closing_time`, `vehicle_types`, `created_at`) VALUES (3,3,'Thamel Tourist Parking Hub','Thamel, Kathmandu','Kathmandu',27.7154,85.3123,'Secure parking hub for tourists and visitors near Thamel.',60.00,'00:00','23:59','car,bike','2026-09-24 02:27:56');
INSERT IGNORE INTO `parking_areas` (`id`, `owner_id`, `name`, `address`, `city`, `latitude`, `longitude`, `description`, `price_per_hour`, `opening_time`, `closing_time`, `vehicle_types`, `created_at`) VALUES (4,3,'Patan Durbar Square Parking','Mangal Bazaar, Lalitpur','Lalitpur',27.6727,85.3247,'Heritage-area parking with easy access to Patan Durbar Square.',35.00,'06:00','20:00','car,bike','2026-09-24 02:27:56');
INSERT IGNORE INTO `parking_areas` (`id`, `owner_id`, `name`, `address`, `city`, `latitude`, `longitude`, `description`, `price_per_hour`, `opening_time`, `closing_time`, `vehicle_types`, `created_at`) VALUES (5,2,'Pulchowk Office Park','Pulchowk, Lalitpur','Lalitpur',27.6788,85.3157,'Weekday office-hours parking for the Pulchowk corporate corridor.',45.00,'08:00','19:00','car','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (1,1,'B01','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (2,1,'B02','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (3,1,'B03','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (4,1,'A04','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (5,1,'A05','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (6,1,'A06','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (7,1,'A07','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (8,1,'A08','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (9,1,'A09','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (10,1,'A10','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (11,1,'A11','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (12,1,'A12','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (13,2,'B01','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (14,2,'B02','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (15,2,'B03','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (16,2,'A04','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (17,2,'A05','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (18,2,'A06','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (19,2,'A07','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (20,2,'A08','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (21,2,'A09','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (22,2,'A10','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (23,3,'B01','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (24,3,'B02','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (25,3,'A03','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (26,3,'A04','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (27,3,'A05','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (28,3,'A06','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (29,3,'A07','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (30,3,'A08','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (31,4,'B01','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (32,4,'B02','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (33,4,'B03','bike','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (34,4,'A04','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (35,4,'A05','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (36,4,'A06','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (37,4,'A07','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (38,4,'A08','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (39,4,'A09','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (40,4,'A10','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (41,5,'A01','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (42,5,'A02','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (43,5,'A03','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (44,5,'A04','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (45,5,'A05','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (46,5,'A06','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (47,5,'A07','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (48,5,'A08','car','available','2026-09-24 02:27:56');
INSERT IGNORE INTO `slots` (`id`, `parking_area_id`, `slot_number`, `vehicle_type`, `status`, `created_at`) VALUES (49,5,'A09','car','available','2026-09-24 02:27:56');
