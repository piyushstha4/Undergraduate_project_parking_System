using SmartParking.Api.Data;
using SmartParking.Api.Infrastructure;
using SmartParking.Api.Models;

namespace SmartParking.Api.Services
{
    public sealed class ReviewService
    {
        private readonly ParkingDb _db;

        public ReviewService(ParkingDb db)
        {
            _db = db;
        }

        public void Create(int areaId, int userId, CreateReviewRequest request)
        {
            if (request == null || request.Rating < 1 || request.Rating > 5)
            {
                throw ApiException.BadRequest("Rating must be between 1 and 5.");
            }

            var area = _db.Scalar("SELECT id FROM parking_areas WHERE id = @p0", areaId);
            if (area == null || area == System.DBNull.Value)
            {
                throw ApiException.NotFound("Parking area not found.");
            }

            var hasBooking = _db.Scalar(
                @"SELECT b.id FROM bookings b JOIN slots s ON s.id = b.slot_id
                  WHERE s.parking_area_id = @p0 AND b.user_id = @p1 AND b.status IN ('confirmed','completed') LIMIT 1",
                areaId,
                userId);
            if (hasBooking == null || hasBooking == System.DBNull.Value)
            {
                throw ApiException.Forbidden("You can only review parking areas you have booked.");
            }

            _db.Execute(
                "INSERT INTO reviews (user_id, parking_area_id, rating, comment) VALUES (@p0, @p1, @p2, @p3)",
                userId,
                areaId,
                request.Rating,
                string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim());
        }
    }
}
