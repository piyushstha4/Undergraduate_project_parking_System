using System;
using System.Threading;
using SmartParking.Api.Services;

namespace SmartParking.Api.Infrastructure
{
    public sealed class BookingReleaseTimer : IDisposable
    {
        private readonly BookingService _bookings;
        private readonly Timer _timer;

        public BookingReleaseTimer(BookingService bookings)
        {
            _bookings = bookings;
            _timer = new Timer(_ =>
            {
                try
                {
                    _bookings.ReleaseExpired();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError(ex.ToString());
                }
            }, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
