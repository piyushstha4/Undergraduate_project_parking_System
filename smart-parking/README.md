# Smart Parking Slot Booking and Management System

**React + C# ASP.NET Core 8 Web API + MySQL**

The original Node.js/Express + SQLite backend has been rewritten in C# (ASP.NET Core 8 Web API)
and now stores its data in MySQL. The React frontend was already React, so it's unchanged. The new
API keeps the same URLs and the same JSON field names, so every page works without edits.

The app is a mobile-responsive web app, so it works in a phone's browser (see
[Open it on your phone](#open-it-on-your-phone)).

```
smart-parking/
├── backend/                          ASP.NET Core 8 Web API (C#)
│   ├── SmartParking.sln              open this in Visual Studio 2022
│   └── SmartParking.Api/
│       ├── Program.cs                startup: JSON, JWT auth, CORS, error handler, background job
│       ├── appsettings.json          MySQL connection string, JWT secret
│       ├── Controllers/              one controller per old routes/*.js file
│       ├── Models/                   table row classes, request & response DTOs
│       ├── Data/                     Db.cs (connection), DbInitializer.cs (create + seed), schema.sql
│       ├── Auth/                     JWT token creation + [Authorize] setup
│       ├── Services/                 auto-release of expired bookings (BackgroundService)
│       └── Infrastructure/           shared controller base class, Swagger
├── database/
│   └── smart_parking.sql             optional full MySQL script (schema + demo data)
└── frontend/                         React (Vite) + Tailwind CSS (unchanged)
```

## Requirements

| Tool | Version | Notes |
|------|---------|-------|
| .NET SDK | 8.0 | Included with **Visual Studio 2022** (ASP.NET workload), or install from dot.net |
| MySQL | 8.0+ | **Or** MariaDB 10.4+ through **XAMPP** / WampServer, both work |
| Node.js | 18+ | Only for running the React frontend |

## 1. MySQL

Use the MySQL already installed on this PC (port **3306**). The database name is `parking`.
Connection settings are in the project `.env` file (`MYSQL_HOST`, `MYSQL_PORT`, `MYSQL_DATABASE`,
`MYSQL_USER`, `MYSQL_PASSWORD`). The C# API reads that file on startup. The React app reads
`VITE_API_BASE_URL` from the same file and calls the API at `http://localhost:4000/api`.

The database already exists. Create the tables and demo accounts by running `database/parking.sql`
in MySQL Workbench or phpMyAdmin, or start the API once with `DB_CREATE_AND_SEED=true`.

## 2. Run the backend (C#)

**Visual Studio 2022:** open `backend/SmartParking.sln`, choose the **http** profile, and press **F5**.
Swagger opens at `http://localhost:4000/swagger`, where you can try every endpoint.

**Command line:**

```bash
cd backend/SmartParking.Api
dotnet run
```

The API listens on `http://localhost:4000`, the same port as the old Node backend.

## 3. Run the frontend (React)

```bash
cd frontend
npm install
npm run dev          # http://localhost:5173
```

The app reads `VITE_API_BASE_URL` from the project `.env` (`http://localhost:4000/api`).

### Accounts

| Role | How they get a login | What they do |
|------|----------------------|--------------|
| Admin | `admin@gmail.com` / `admin@123` | User management: creates parking owners, sets their passwords, and can suspend or reactivate any driver or owner |
| Parking owner | Username and password are created by the admin and given to the owner | Adds parking areas and slots, sees bookings, and reads reports for their own areas |
| Driver | Registers at Sign up | Searches parking, books a slot, pays, and can cancel their own booking |

## Open it on your phone

Your phone and PC must be on the same Wi-Fi. Find your PC's IP with `ipconfig` (for example `192.168.1.10`).

1. Run the API so it accepts network connections:
   `dotnet run --urls http://0.0.0.0:4000`
2. In `.env`, set `VITE_API_BASE_URL=http://192.168.1.10:4000/api`
3. Run `npm run dev:mobile` in `frontend/`
4. On the phone, open `http://192.168.1.10:5173`

If the phone can't connect, allow ports 4000 and 5173 through Windows Firewall.

## How the Node code maps to C#

| Old Node file | New C# file | Endpoints |
|---|---|---|
| `server.js` | `Program.cs` | `/api/health`, CORS, error handler, 404 |
| `middleware/auth.js` | `Auth/JwtSetup.cs`, `Auth/TokenService.cs` | `authenticate` → `[Authorize]`, `authorize(...)` → `[Authorize(Roles = "...")]` |
| `db/schema.sql`, `db/index.js` | `Data/schema.sql`, `Data/Db.cs` | SQLite → MySQL |
| `db/seed.js` | `Data/DbInitializer.cs` | demo data |
| `routes/auth.js` | `Controllers/AuthController.cs` | `POST register`, `POST login`, `GET me` |
| `routes/parkingAreas.js` | `Controllers/ParkingAreasController.cs` | search/filter, detail, create, update, delete, `owner/mine` |
| `routes/slots.js` | `Controllers/SlotsController.cs` | `POST /parking-areas/{id}/slots`, `PUT/DELETE /slots/{id}` |
| `routes/bookings.js` | `Controllers/BookingsController.cs` | book + simulated payment, `me`, cancel, owner/admin list |
| `routes/reviews.js` | `Controllers/ReviewsController.cs` | `POST /parking-areas/{id}/reviews` |
| `routes/admin.js` | `Controllers/AdminController.cs` | stats, users, suspend/activate, all areas |
| `routes/owner.js` | `Controllers/OwnerController.cs` | reports |

Libraries used: **MySqlConnector** (MySQL driver), **Dapper** (maps SQL rows to C# classes),
**BCrypt.Net-Next** (password hashing), **JwtBearer** (login tokens) and **Swashbuckle** (Swagger UI).
Visual Studio restores them automatically from NuGet.

### SQLite → MySQL type changes

| SQLite | MySQL |
|---|---|
| `INTEGER PRIMARY KEY AUTOINCREMENT` | `INT AUTO_INCREMENT PRIMARY KEY` |
| `TEXT` + `CHECK (role IN (...))` | `ENUM('user','parking_owner','admin')` |
| `REAL` (money) | `DECIMAL(10,2)` |
| `TEXT` dates | `DATETIME`, always stored in UTC |

## Improvements over the Node version (useful for your report)

- **Booking concurrency.** A booking runs inside a MySQL transaction that locks the slot row with
  `SELECT ... FOR UPDATE` before checking for overlaps. Two people booking the same slot at the same
  moment can't both succeed. Tested with 20 simultaneous requests: 1 succeeded and 19 were rejected with 409.
- **Real background job for auto-release.** `ExpiredBookingWorker` (a .NET `BackgroundService`)
  runs every minute. It marks finished bookings `completed` and frees their slots. The Node demo
  only did this when a request came in; the API still does that as well.
- **Correct time zones.** All times are stored in UTC and returned with a `Z` suffix, so the React
  app shows them in the user's local time. Nepal is UTC+5:45.
- **Stricter input checks.** Updating a slot with an invalid status or vehicle type returns a clear
  400 error instead of a server error.

## Testing done

Both backends were run side by side from the same demo data, and the same 95 API requests were
sent to each. Every request returned the same status code and the same data. The only
differences were the order of rows created in the same second: the C# API lists the newest first.
NuGet couldn't be reached from the build machine, so during that test the JWT login library was
swapped for a small stand-in that creates identical tokens. After you open the project, check login
first in Swagger (`POST /api/auth/login`, then **Authorize**, then `GET /api/auth/me`).
The React app was also tested in a phone-sized browser against the C# API. Login, search, booking,
My Bookings and the admin dashboard all worked, with no console errors.

## Known simplifications (future work)

- Payments (eSewa / Khalti / Card) are simulated. The single place to add a real gateway is marked
  in `BookingsController.Create`.
- No refunds, no email/SMS notifications, and no map/GPS, as in the original scope.
- Change `JWT_SECRET` in `.env` before deploying anywhere public.
