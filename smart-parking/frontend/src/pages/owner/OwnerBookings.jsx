import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, EmptyState, Spinner, StatusBadge } from '../../components/ui';
import { money } from '../../config';

function formatRange(start, end) {
  const s = new Date(start);
  const e = new Date(end);
  const opts = { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' };
  return `${s.toLocaleString([], opts)} → ${e.toLocaleString([], opts)}`;
}

export default function OwnerBookings() {
  const [bookings, setBookings] = useState(null);
  const [areas, setAreas] = useState([]);
  const [error, setError] = useState('');
  const [areaFilter, setAreaFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [cancellingId, setCancellingId] = useState(null);

  function load() {
    const params = {};
    if (areaFilter) params.areaId = areaFilter;
    if (statusFilter) params.status = statusFilter;
    api
      .get('/bookings', { params })
      .then(({ data }) => setBookings(data.bookings))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(() => {
    api.get('/parking-areas/owner/mine').then(({ data }) => setAreas(data.areas));
  }, []);

  useEffect(load, [areaFilter, statusFilter]);

  async function handleCancel(id) {
    setCancellingId(id);
    try {
      await api.put(`/bookings/${id}/cancel`);
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setCancellingId(null);
    }
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">Bookings</h1>
      <p className="text-sm text-gray-500 mb-6">All bookings across your parking areas.</p>

      <Card className="p-4 mb-6 flex flex-col sm:flex-row gap-3">
        <select
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm flex-1"
          value={areaFilter}
          onChange={(e) => setAreaFilter(e.target.value)}
        >
          <option value="">All parking areas</option>
          {areas.map((a) => (
            <option key={a.id} value={a.id}>{a.name}</option>
          ))}
        </select>
        <select
          className="rounded-lg border border-gray-300 px-3 py-2 text-sm flex-1"
          value={statusFilter}
          onChange={(e) => setStatusFilter(e.target.value)}
        >
          <option value="">All statuses</option>
          <option value="confirmed">Confirmed</option>
          <option value="completed">Completed</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </Card>

      {error && (
        <div className="mb-4">
          <Alert type="error" onClose={() => setError('')}>{error}</Alert>
        </div>
      )}

      {bookings === null ? (
        <div className="flex justify-center py-20 text-brand-600">
          <Spinner />
        </div>
      ) : bookings.length === 0 ? (
        <EmptyState title="No bookings found" subtitle="Try changing your filters." />
      ) : (
        <div className="space-y-3">
          {bookings.map((b) => (
            <Card key={b.id} className="p-4">
              <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                <div>
                  <div className="flex items-center gap-2 flex-wrap">
                    <h3 className="font-semibold text-gray-900">{b.area_name}</h3>
                    <StatusBadge status={b.status} />
                  </div>
                  <p className="text-sm text-gray-500">Slot {b.slot_number} · {b.user_name} ({b.user_email})</p>
                  <p className="text-sm text-gray-600 mt-1">{formatRange(b.start_time, b.end_time)}</p>
                </div>
                <div className="text-right shrink-0">
                  <p className="font-semibold text-gray-900">{money(b.total_amount)}</p>
                  {b.status === 'confirmed' && (
                    <Button
                      variant="danger"
                      className="mt-2"
                      disabled={cancellingId === b.id}
                      onClick={() => handleCancel(b.id)}
                    >
                      {cancellingId === b.id ? 'Cancelling…' : 'Cancel booking'}
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
