import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, Input, Spinner, StatusBadge } from '../../components/ui';

export default function AreaSlots() {
  const { id } = useParams();
  const [data, setData] = useState(null);
  const [error, setError] = useState('');
  const [newSlot, setNewSlot] = useState({ slot_number: '', vehicle_type: 'car' });
  const [adding, setAdding] = useState(false);
  const [busySlotId, setBusySlotId] = useState(null);

  function load() {
    api
      .get(`/parking-areas/${id}`)
      .then(({ data }) => setData(data))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, [id]);

  async function handleAddSlot(e) {
    e.preventDefault();
    if (!newSlot.slot_number.trim()) return;
    setAdding(true);
    setError('');
    try {
      await api.post(`/parking-areas/${id}/slots`, newSlot);
      setNewSlot({ slot_number: '', vehicle_type: 'car' });
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setAdding(false);
    }
  }

  async function handleStatusChange(slotId, status) {
    setBusySlotId(slotId);
    setError('');
    try {
      await api.put(`/slots/${slotId}`, { status });
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setBusySlotId(null);
    }
  }

  async function handleDeleteSlot(slotId) {
    if (!window.confirm('Delete this slot?')) return;
    setBusySlotId(slotId);
    setError('');
    try {
      await api.delete(`/slots/${slotId}`);
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setBusySlotId(null);
    }
  }

  if (!data && !error) {
    return (
      <div className="flex justify-center py-20 text-brand-600">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="max-w-4xl mx-auto px-4 py-6">
      <Link to="/owner/areas" className="text-sm text-brand-600 hover:underline">← My parking areas</Link>

      {error && (
        <div className="my-4">
          <Alert type="error" onClose={() => setError('')}>{error}</Alert>
        </div>
      )}

      {data && (
        <>
          <h1 className="text-2xl font-semibold text-gray-900 mt-3">{data.area.name}</h1>
          <p className="text-sm text-gray-500 mb-6">{data.area.address}, {data.area.city}</p>

          <Card className="p-5 mb-6">
            <h2 className="font-semibold text-gray-900 mb-3">Add a slot</h2>
            <form onSubmit={handleAddSlot} className="flex flex-col sm:flex-row gap-3 sm:items-end">
              <div className="flex-1">
                <Input
                  label="Slot number"
                  placeholder="e.g. A13"
                  value={newSlot.slot_number}
                  onChange={(e) => setNewSlot({ ...newSlot, slot_number: e.target.value })}
                  required
                />
              </div>
              <div className="flex-1">
                <span className="block text-sm font-medium text-gray-700 mb-1">Vehicle type</span>
                <select
                  className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                  value={newSlot.vehicle_type}
                  onChange={(e) => setNewSlot({ ...newSlot, vehicle_type: e.target.value })}
                >
                  <option value="car">Car</option>
                  <option value="bike">Bike</option>
                </select>
              </div>
              <Button type="submit" disabled={adding}>{adding ? 'Adding…' : 'Add slot'}</Button>
            </form>
          </Card>

          <Card className="p-5">
            <h2 className="font-semibold text-gray-900 mb-3">Slots ({data.slots.length})</h2>
            {data.slots.length === 0 ? (
              <p className="text-sm text-gray-500">No slots yet — add one above.</p>
            ) : (
              <div className="divide-y">
                {data.slots.map((slot) => (
                  <div key={slot.id} className="py-3 flex flex-wrap items-center justify-between gap-2">
                    <div className="flex items-center gap-3">
                      <span className="font-medium text-gray-900 w-14">{slot.slot_number}</span>
                      <span className="text-xs text-gray-500 capitalize">{slot.vehicle_type}</span>
                      <StatusBadge status={slot.status} />
                    </div>
                    <div className="flex items-center gap-2">
                      <select
                        className="rounded-lg border border-gray-300 px-2 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-brand-400"
                        value={slot.status}
                        disabled={busySlotId === slot.id}
                        onChange={(e) => handleStatusChange(slot.id, e.target.value)}
                      >
                        <option value="available">Available</option>
                        <option value="maintenance">Maintenance</option>
                        <option value="occupied">Occupied</option>
                      </select>
                      <Button
                        variant="danger"
                        className="px-2 py-1.5 text-xs"
                        disabled={busySlotId === slot.id}
                        onClick={() => handleDeleteSlot(slot.id)}
                      >
                        Delete
                      </Button>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </Card>
        </>
      )}
    </div>
  );
}
