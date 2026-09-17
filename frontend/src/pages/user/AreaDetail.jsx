import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, Spinner, StatusBadge, Input } from '../../components/ui';

function toLocalInputValue(date) {
  const pad = (n) => String(n).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

export default function AreaDetail() {
  const { id } = useParams();
  const { user } = useAuth();
  const navigate = useNavigate();

  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [selectedSlot, setSelectedSlot] = useState(null);
  const [start, setStart] = useState(() => toLocalInputValue(new Date(Date.now() + 10 * 60 * 1000)));
  const [end, setEnd] = useState(() => toLocalInputValue(new Date(Date.now() + 2 * 60 * 60 * 1000)));
  const [vehiclePlate, setVehiclePlate] = useState('');
  const [paymentMethod, setPaymentMethod] = useState('esewa');
  const [booking, setBooking] = useState(false);
  const [bookError, setBookError] = useState('');
  const [success, setSuccess] = useState(null);

  function load() {
    setError('');
    api
      .get(`/parking-areas/${id}`)
      .then(({ data }) => setData(data))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, [id]);

  const hours = Math.max(1, Math.ceil((new Date(end) - new Date(start)) / (1000 * 60 * 60)));
  const estimate = data ? Math.round(hours * data.area.price_per_hour * 100) / 100 : 0;

  async function handleBook(e) {
    e.preventDefault();
    if (!user) {
      navigate('/login', { state: { from: { pathname: `/areas/${id}` } } });
      return;
    }
    if (!selectedSlot) {
      setBookError('Please select a slot first.');
      return;
    }
    setBookError('');
    setBooking(true);
    try {
      const { data: res } = await api.post('/bookings', {
        slot_id: selectedSlot.id,
        start_time: new Date(start).toISOString(),
        end_time: new Date(end).toISOString(),
        vehicle_plate: vehiclePlate || undefined,
        payment_method: paymentMethod,
      });
      setSuccess(res.booking);
      setSelectedSlot(null);
      load();
    } catch (err) {
      setBookError(apiErrorMessage(err));
    } finally {
      setBooking(false);
    }
  }

  if (error) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-8">
        <Alert type="error">{error}</Alert>
      </div>
    );
  }

  if (!data) {
    return (
      <div className="flex justify-center py-20 text-brand-600">
        <Spinner />
      </div>
    );
  }

  const { area, slots, reviews, avgRating } = data;
  const vehicleTypes = area.vehicle_types.split(',');

  return (
    <div className="max-w-6xl mx-auto px-4 py-6">
      <button onClick={() => navigate(-1)} className="text-sm text-brand-600 hover:underline mb-4">
        ← Back to search
      </button>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <Card className="p-5">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h1 className="text-xl font-semibold text-gray-900">{area.name}</h1>
                <p className="text-sm text-gray-500 mt-1">{area.address}, {area.city}</p>
              </div>
              {avgRating && (
                <span className="text-sm font-medium text-amber-600 whitespace-nowrap">★ {avgRating} ({reviews.length})</span>
              )}
            </div>
            {area.description && <p className="text-sm text-gray-600 mt-3">{area.description}</p>}
            <div className="flex flex-wrap gap-4 mt-4 text-sm text-gray-600">
              <span>💰 Rs. {area.price_per_hour}/hr</span>
              <span>🕒 {area.opening_time} – {area.closing_time}</span>
              <span>🚗 {vehicleTypes.join(', ')}</span>
              <span>{area.available_slots}/{area.total_slots} slots free</span>
            </div>
          </Card>

          <Card className="p-5">
            <h2 className="font-semibold text-gray-900 mb-3">Select a slot</h2>
            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-6 gap-2">
              {slots.map((slot) => {
                const disabled = slot.status !== 'available';
                const isSelected = selectedSlot?.id === slot.id;
                return (
                  <button
                    key={slot.id}
                    disabled={disabled}
                    onClick={() => setSelectedSlot(slot)}
                    className={`rounded-lg border py-3 text-sm font-medium transition-colors ${
                      disabled
                        ? 'bg-gray-100 text-gray-400 border-gray-200 cursor-not-allowed'
                        : isSelected
                        ? 'bg-brand-600 text-white border-brand-600'
                        : 'bg-white text-gray-800 border-gray-300 hover:border-brand-400'
                    }`}
                    title={`${slot.slot_number} · ${slot.vehicle_type} · ${slot.status}`}
                  >
                    {slot.slot_number}
                  </button>
                );
              })}
            </div>
            <div className="flex gap-4 mt-4 text-xs text-gray-500">
              <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-white border border-gray-300 inline-block" /> Available</span>
              <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-gray-200 inline-block" /> Unavailable</span>
              <span className="flex items-center gap-1"><span className="w-3 h-3 rounded bg-brand-600 inline-block" /> Selected</span>
            </div>
          </Card>

          {reviews.length > 0 && (
            <Card className="p-5">
              <h2 className="font-semibold text-gray-900 mb-3">Reviews</h2>
              <div className="space-y-3">
                {reviews.slice(0, 5).map((r) => (
                  <div key={r.id} className="border-b last:border-0 pb-3 last:pb-0">
                    <div className="flex items-center justify-between">
                      <span className="text-sm font-medium text-gray-800">{r.user_name}</span>
                      <span className="text-amber-600 text-sm">{'★'.repeat(r.rating)}</span>
                    </div>
                    {r.comment && <p className="text-sm text-gray-600 mt-1">{r.comment}</p>}
                  </div>
                ))}
              </div>
            </Card>
          )}
        </div>

        <div>
          <Card className="p-5 sticky top-20">
            <h2 className="font-semibold text-gray-900 mb-3">Book this slot</h2>

            {success ? (
              <div className="space-y-3">
                <Alert type="success">Booking confirmed! Slot {success.slot_number} is reserved for you.</Alert>
                <div className="text-sm text-gray-600 space-y-1">
                  <p>Total paid: <strong>Rs. {success.total_amount}</strong></p>
                  <p>Status: <StatusBadge status={success.status} /></p>
                </div>
                <Button className="w-full" onClick={() => navigate('/bookings')}>
                  View my bookings
                </Button>
                <Button variant="secondary" className="w-full" onClick={() => setSuccess(null)}>
                  Book another slot
                </Button>
              </div>
            ) : (
              <form onSubmit={handleBook} className="space-y-3">
                {bookError && <Alert type="error">{bookError}</Alert>}

                <p className="text-sm text-gray-600">
                  Selected slot: <strong>{selectedSlot ? selectedSlot.slot_number : 'None yet'}</strong>
                </p>

                <Input label="Start time" type="datetime-local" value={start} onChange={(e) => setStart(e.target.value)} required />
                <Input label="End time" type="datetime-local" value={end} onChange={(e) => setEnd(e.target.value)} required />
                <Input label="Vehicle plate (optional)" value={vehiclePlate} onChange={(e) => setVehiclePlate(e.target.value)} placeholder="BA 1 PA 1234" />

                <div>
                  <span className="block text-sm font-medium text-gray-700 mb-1">Payment method</span>
                  <select
                    className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                    value={paymentMethod}
                    onChange={(e) => setPaymentMethod(e.target.value)}
                  >
                    <option value="esewa">eSewa</option>
                    <option value="khalti">Khalti</option>
                    <option value="card">Card</option>
                  </select>
                </div>

                <div className="border-t pt-3 flex items-center justify-between text-sm">
                  <span className="text-gray-500">{hours} hour{hours > 1 ? 's' : ''} × Rs. {area.price_per_hour}</span>
                  <span className="font-semibold text-gray-900">Rs. {estimate}</span>
                </div>

                <Button type="submit" disabled={booking} className="w-full">
                  {booking ? 'Processing payment…' : user ? `Pay & Book — Rs. ${estimate}` : 'Log in to book'}
                </Button>
                <p className="text-xs text-gray-400 text-center">
                  Payment is simulated for this demo build — no real transaction occurs.
                </p>
              </form>
            )}
          </Card>
        </div>
      </div>
    </div>
  );
}
