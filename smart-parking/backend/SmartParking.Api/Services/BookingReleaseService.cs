using Dapper;
using SmartParking.Api.Data;

namespace SmartParking.Api.Services;

/// <summary>
/// Auto slot release (Objective #3): confirmed bookings whose end_time has passed are
/// marked 'completed' and their slot is set back to 'available'.
/// </summary>
public class BookingReleaseService
{
    private readonly Db _db;
    public BookingReleaseService(Db db) => _db = db;

    public async Task<int> ReleaseExpiredBookingsAsync()
    {
        await using var conn = await _db.OpenAsync();
        await using var tx = await conn.BeginTransactionAsync();

        var expired = (await conn.QueryAsync<ExpiredBooking>(
            "SELECT id, slot_id FROM bookings WHERE status = 'confirmed' AND end_time <= UTC_TIMESTAMP() FOR UPDATE",
            transaction: tx)).ToList();

        foreach (var b in expired)
        {
            await conn.ExecuteAsync("UPDATE bookings SET status = 'completed' WHERE id = @Id", new { b.Id }, tx);
            await conn.ExecuteAsync("UPDATE slots SET status = 'available' WHERE id = @SlotId", new { b.SlotId }, tx);
        }

        await tx.CommitAsync();
        return expired.Count;
    }

    public sealed class ExpiredBooking
    {
        public int Id { get; set; }
        public int SlotId { get; set; }
    }
}

/// <summary>
/// Runs the release job every minute in the background. This replaces the Node demo's
/// "check on every request" workaround with a real scheduled job (IHostedService).
/// </summary>
public class ExpiredBookingWorker : BackgroundService
{
    private readonly BookingReleaseService _service;
    private readonly ILogger<ExpiredBookingWorker> _logger;

    public ExpiredBookingWorker(BookingReleaseService service, ILogger<ExpiredBookingWorker> logger)
    {
        _service = service;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1));
        do
        {
            try
            {
                var released = await _service.ReleaseExpiredBookingsAsync();
                if (released > 0) _logger.LogInformation("Auto-released {Count} expired booking(s).", released);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release expired bookings.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
