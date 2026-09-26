using SmartParking.Api.Data;
using SmartParking.Api.Services;

namespace SmartParking.Api
{
    public static class AppServices
    {
        public static ParkingDb Db { get; private set; }
        public static AuthService Auth { get; private set; }
        public static ParkingAreaService Areas { get; private set; }
        public static SlotService Slots { get; private set; }
        public static BookingService Bookings { get; private set; }
        public static AdminService Admin { get; private set; }
        public static OwnerService Owner { get; private set; }
        public static ReviewService Reviews { get; private set; }

        public static void Initialize(ParkingDb db)
        {
            Db = db;
            Auth = new AuthService(db);
            Areas = new ParkingAreaService(db);
            Slots = new SlotService(db, Areas);
            Bookings = new BookingService(db);
            Admin = new AdminService(db);
            Owner = new OwnerService(db);
            Reviews = new ReviewService(db);
        }
    }
}
