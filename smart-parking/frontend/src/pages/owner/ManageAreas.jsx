import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, EmptyState, Input, Spinner } from '../../components/ui';
import { config, money } from '../../config';

const emptyForm = {
  name: '', address: '', city: '', description: '',
  price_per_hour: '', capacity: '', opening_time: '06:00', closing_time: '22:00', vehicle_types: 'car,bike',
};

export default function ManageAreas() {
  const [areas, setAreas] = useState(null);
  const [error, setError] = useState('');
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState(emptyForm);
  const [saving, setSaving] = useState(false);
  const [formError, setFormError] = useState('');

  function load() {
    api
      .get('/parking-areas/owner/mine')
      .then(({ data }) => setAreas(data.areas))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, []);

  async function handleCreate(e) {
    e.preventDefault();
    setFormError('');
    setSaving(true);
    try {
      await api.post('/parking-areas', {
        ...form,
        price_per_hour: Number(form.price_per_hour),
        capacity: Number(form.capacity),
      });
      setForm(emptyForm);
      setShowForm(false);
      load();
    } catch (err) {
      setFormError(apiErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id) {
    if (!window.confirm('Delete this parking area? This cannot be undone.')) return;
    setError('');
    try {
      await api.delete(`/parking-areas/${id}`);
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    }
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">My Parking Areas</h1>
          <p className="text-sm text-gray-500 mt-1">Add facilities and manage their slots.</p>
        </div>
        <Button onClick={() => setShowForm((s) => !s)}>{showForm ? 'Close' : '+ Add Area'}</Button>
      </div>

      {error && (
        <div className="mb-4">
          <Alert type="error" onClose={() => setError('')}>{error}</Alert>
        </div>
      )}

      {showForm && (
        <Card className="p-5 mb-6">
          <h2 className="font-semibold text-gray-900 mb-3">New parking area</h2>
          {formError && (
            <div className="mb-3">
              <Alert type="error">{formError}</Alert>
            </div>
          )}
          <form onSubmit={handleCreate} className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Input label="Name" required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
            <Input label="City" required value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} />
            <Input
              label="Address"
              required
              className="sm:col-span-2"
              value={form.address}
              onChange={(e) => setForm({ ...form, address: e.target.value })}
            />
            <Input
              label={`Price per hour (${config.currencyLabel})`}
              type="number"
              min="0"
              required
              value={form.price_per_hour}
              onChange={(e) => setForm({ ...form, price_per_hour: e.target.value })}
            />
            <Input
              label="Capacity (vehicles)"
              type="number"
              min="1"
              max="500"
              required
              value={form.capacity}
              onChange={(e) => setForm({ ...form, capacity: e.target.value })}
            />
            <div>
              <span className="block text-sm font-medium text-gray-700 mb-1">Vehicle types</span>
              <select
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                value={form.vehicle_types}
                onChange={(e) => setForm({ ...form, vehicle_types: e.target.value })}
              >
                <option value="car,bike">Car & Bike</option>
                <option value="car">Car only</option>
                <option value="bike">Bike only</option>
              </select>
            </div>
            <Input label="Opening time" type="time" value={form.opening_time} onChange={(e) => setForm({ ...form, opening_time: e.target.value })} />
            <Input label="Closing time" type="time" value={form.closing_time} onChange={(e) => setForm({ ...form, closing_time: e.target.value })} />
            <label className="block sm:col-span-2">
              <span className="block text-sm font-medium text-gray-700 mb-1">Description (optional)</span>
              <textarea
                className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
                rows={2}
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
              />
            </label>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={saving}>{saving ? 'Saving…' : 'Create parking area'}</Button>
            </div>
          </form>
        </Card>
      )}

      {areas === null ? (
        <div className="flex justify-center py-20 text-brand-600">
          <Spinner />
        </div>
      ) : areas.length === 0 ? (
        <EmptyState title="No parking areas yet" subtitle="Add your first facility to start accepting bookings." />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {areas.map((a) => (
            <Card key={a.id} className="p-4">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-gray-900">{a.name}</h3>
                  <p className="text-sm text-gray-500">{a.address}, {a.city}</p>
                </div>
                <span className="text-sm font-medium text-brand-700">{money(a.price_per_hour)}/hr</span>
              </div>
              <p className="text-sm text-gray-600 mt-2">Capacity: {a.capacity} vehicles · {a.available_slots}/{a.total_slots} slots available</p>
              <div className="flex gap-2 mt-4">
                <Link to={`/owner/areas/${a.id}/slots`}>
                  <Button variant="secondary">Manage slots</Button>
                </Link>
                <Button variant="danger" onClick={() => handleDelete(a.id)}>Delete</Button>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
