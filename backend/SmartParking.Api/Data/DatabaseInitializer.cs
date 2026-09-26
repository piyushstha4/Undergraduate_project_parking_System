using System;
using System.Globalization;
using System.IO;
using BCrypt.Net;

namespace SmartParking.Api.Data
{
    public static class DatabaseInitializer
    {
        public static ParkingDb EnsureCreated()
        {
            var databasePath = ResolveDatabasePath();
            var db = new ParkingDb(databasePath);

            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "schema.sql");
            var schema = File.ReadAllText(schemaPath);
            using (var connection = db.Open())
            using (var command = connection.CreateCommand())
            {
                command.CommandText = schema;
                command.ExecuteNonQuery();
            }

            SeedIfEmpty(db);
            return db;
        }

        private static string ResolveDatabasePath()
        {
            var configured = System.Configuration.ConfigurationManager.AppSettings["DbPath"];
            if (!string.IsNullOrWhiteSpace(configured))
            {
                return Path.GetFullPath(configured);
            }

            var fromOutput = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "data", "parking.db"));
            return fromOutput;
        }

        private static void SeedIfEmpty(ParkingDb db)
        {
            var count = Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM users"));
            if (count > 0)
            {
                Console.WriteLine("Database already has data — skipping seed.");
                return;
            }

            Console.WriteLine("Seeding demo data...");
            db.Write<object>((connection, transaction) =>
            {
                long InsertUser(string name, string email, string phone, string password, string role)
                {
                    ParkingDb.Exec(
                        connection,
                        transaction,
                        "INSERT INTO users (name, email, phone, password_hash, role) VALUES (@p0, @p1, @p2, @p3, @p4)",
                        name,
                        email,
                        phone,
                        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 10),
                        role);
                    return ParkingDb.LastInsertId(connection, transaction);
                }

                InsertUser("System Admin", "admin@smartparking.com", "9800000000", "Admin@123", "admin");
                var owner1Id = InsertUser("Ramesh Shrestha", "owner1@smartparking.com", "9801111111", "Owner@123", "parking_owner");
                var owner2Id = InsertUser("Sita Maharjan", "owner2@smartparking.com", "9802222222", "Owner@123", "parking_owner");
                InsertUser("Piyush Shrestha", "user@smartparking.com", "9803333333", "User@123", "user");
                InsertUser("Anisha Gurung", "anisha@smartparking.com", "9804444444", "User@123", "user");

                var areas = new[]
                {
                    new AreaSeed(owner1Id, "City Center Mall Parking", "Kamalpokhari, Kathmandu", "Kathmandu", 27.7089, 85.3247, "Multi-level covered parking beside City Center Mall. CCTV monitored.", 50, "07:00", "22:00", "car,bike", 12),
                    new AreaSeed(owner1Id, "New Road Business Parking", "New Road, Kathmandu", "Kathmandu", 27.7040, 85.3090, "Open-air parking close to New Road shopping street. High demand during peak hours.", 40, "06:00", "21:00", "car,bike", 10),
                    new AreaSeed(owner2Id, "Thamel Tourist Parking Hub", "Thamel, Kathmandu", "Kathmandu", 27.7154, 85.3123, "Secure parking hub for tourists and visitors near Thamel.", 60, "00:00", "23:59", "car,bike", 8),
                    new AreaSeed(owner2Id, "Patan Durbar Square Parking", "Mangal Bazaar, Lalitpur", "Lalitpur", 27.6727, 85.3247, "Heritage-area parking with easy access to Patan Durbar Square.", 35, "06:00", "20:00", "car,bike", 10),
                    new AreaSeed(owner1Id, "Pulchowk Office Park", "Pulchowk, Lalitpur", "Lalitpur", 27.6788, 85.3157, "Weekday office-hours parking for the Pulchowk corporate corridor.", 45, "08:00", "19:00", "car", 9)
                };

                foreach (var area in areas)
                {
                    ParkingDb.Exec(
                        connection,
                        transaction,
                        @"INSERT INTO parking_areas
                          (owner_id, name, address, city, latitude, longitude, description, price_per_hour, opening_time, closing_time, vehicle_types)
                          VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6, @p7, @p8, @p9, @p10)",
                        area.OwnerId, area.Name, area.Address, area.City, area.Lat, area.Lng, area.Description, area.Price, area.Open, area.Close, area.VehicleTypes);

                    var areaId = ParkingDb.LastInsertId(connection, transaction);
                    var bikeCount = (int)Math.Floor(area.Slots * 0.3);
                    for (var i = 1; i <= area.Slots; i++)
                    {
                        var isBike = area.VehicleTypes.IndexOf("bike", StringComparison.OrdinalIgnoreCase) >= 0 && i <= bikeCount;
                        var slotNumber = (isBike ? "B" : "A") + i.ToString("00", CultureInfo.InvariantCulture);
                        ParkingDb.Exec(
                            connection,
                            transaction,
                            "INSERT INTO slots (parking_area_id, slot_number, vehicle_type) VALUES (@p0, @p1, @p2)",
                            areaId,
                            slotNumber,
                            isBike ? "bike" : "car");
                    }
                }

                return null;
            });

            Console.WriteLine("Seed complete.");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Demo accounts:");
            Console.WriteLine("  Admin:          admin@smartparking.com / Admin@123");
            Console.WriteLine("  Parking Owner:  owner1@smartparking.com / Owner@123");
            Console.WriteLine("  Parking Owner:  owner2@smartparking.com / Owner@123");
            Console.WriteLine("  User:           user@smartparking.com / User@123");
            Console.WriteLine("---------------------------------------------");
        }

        private sealed class AreaSeed
        {
            public AreaSeed(long ownerId, string name, string address, string city, double lat, double lng, string description, double price, string open, string close, string vehicleTypes, int slots)
            {
                OwnerId = ownerId;
                Name = name;
                Address = address;
                City = city;
                Lat = lat;
                Lng = lng;
                Description = description;
                Price = price;
                Open = open;
                Close = close;
                VehicleTypes = vehicleTypes;
                Slots = slots;
            }

            public long OwnerId { get; }
            public string Name { get; }
            public string Address { get; }
            public string City { get; }
            public double Lat { get; }
            public double Lng { get; }
            public string Description { get; }
            public double Price { get; }
            public string Open { get; }
            public string Close { get; }
            public string VehicleTypes { get; }
            public int Slots { get; }
        }
    }
}
