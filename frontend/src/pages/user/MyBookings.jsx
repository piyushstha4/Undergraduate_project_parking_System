import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, EmptyState, Spinner, StatusBadge } from '../../components/ui';

function formatRange(start, end) {
  const s = new Date(start);
  const e = new Date(end);
  const opts = { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' };
  return `${s.toLocaleString([], opts)} → ${e.toLocaleString([], opts)}`;
}

export default function MyBookings() {
  const [bookings, setBookings] = useState(null);
  const [error, setError] = useState('');
  const [cancellingId, setCancellingId] = useState(null);

  function load() {
    api
      .get('/bookings/me')
      .then(({ data }) => setBookings(data.bookings))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, []);

  async function handleCancel(id) {
    setCancellingId(id);
    setError('');
    try {
      await api.put(`/bookings/${id}/cancel`);
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setCancellingId(null);
    }
  }

  if (bookings === null) {
    return (
      <div className="flex justify-center py-20 text-brand-600">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">My Bookings</h1>
      <p className="text-sm text-gray-500 mb-6">Your parking reservation history.</p>

      {error && (
        <div className="mb-4">
          <Alert type="error">{error}</Alert>
        </div>
      )}

      {bookings.length === 0 ? (
        <EmptyState
          title="No bookings yet"
          subtitle="Search for a parking area and reserve your first slot."
          action={
            <Link to="/">
              <Button>Find parking</Button>
            </Link>
          }
        />
      ) : (
        <div className="space-y-3">
          {bookings.map((b) => (
            <Card key={b.id} className="p-4">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2">
                    <h3 className="font-semibold text-gray-900">{b.area_name}</h3>
                    <StatusBadge status={b.status} />
                  </div>
                  <p className="text-sm text-gray-500">{b.area_address}, {b.area_city} · Slot {b.slot_number}</p>
                  <p className="text-sm text-gray-600 mt-1">{formatRange(b.start_time, b.end_time)}</p>
                  {b.vehicle_plate && <p className="text-xs text-gray-400 mt-1">Vehicle: {b.vehicle_plate}</p>}
                </div>
                <div className="text-right shrink-0">
                  <p className="font-semibold text-gray-900">Rs. {b.total_amount}</p>
                  {['confirmed', 'pending_payment'].includes(b.status) && (
                    <Button
                      variant="danger"
                      className="mt-2"
                      disabled={cancellingId === b.id}
                      onClick={() => handleCancel(b.id)}
                    >
                      {cancellingId === b.id ? 'Cancelling…' : 'Cancel'}
                    </Button>
                  )}
                </div>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
