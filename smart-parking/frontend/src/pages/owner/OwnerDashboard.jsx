import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Card, Spinner } from '../../components/ui';
import { money } from '../../config';

function StatCard({ label, value, hint }) {
  return (
    <Card className="p-4">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="text-2xl font-semibold text-gray-900 mt-1">{value}</p>
      {hint && <p className="text-xs text-gray-400 mt-1">{hint}</p>}
    </Card>
  );
}

export default function OwnerDashboard() {
  const [report, setReport] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get('/owner/reports')
      .then(({ data }) => setReport(data))
      .catch((err) => setError(apiErrorMessage(err)));
  }, []);

  if (error) {
    return (
      <div className="max-w-5xl mx-auto px-4 py-8">
        <Alert type="error">{error}</Alert>
      </div>
    );
  }

  if (!report) {
    return (
      <div className="flex justify-center py-20 text-brand-600">
        <Spinner />
      </div>
    );
  }

  const { areas, totals } = report;

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">Dashboard</h1>
      <p className="text-sm text-gray-500 mb-6">Overview of your parking facilities.</p>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
        <StatCard label="Parking Areas" value={areas.length} />
        <StatCard label="Total Slots" value={totals.totalSlots} hint={`${totals.availableSlots} available now`} />
        <StatCard label="Active Bookings" value={totals.activeBookings} />
        <StatCard label="Revenue" value={money(totals.revenue)} hint="All confirmed & completed bookings" />
      </div>

      <div className="flex items-center justify-between mb-3">
        <h2 className="font-semibold text-gray-900">Your parking areas</h2>
        <Link to="/owner/areas" className="text-sm text-brand-600 hover:underline">Manage areas →</Link>
      </div>

      {areas.length === 0 ? (
        <Card className="p-6 text-center text-gray-500 text-sm">
          You haven&apos;t added any parking areas yet.{' '}
          <Link to="/owner/areas" className="text-brand-600 hover:underline">Add your first one</Link>.
        </Card>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {areas.map((a) => (
            <Card key={a.id} className="p-4">
              <h3 className="font-medium text-gray-900">{a.name}</h3>
              <p className="text-xs text-gray-500">{a.city}</p>
              <div className="flex justify-between text-sm text-gray-600 mt-3">
                <span>Capacity {a.capacity} · {a.available_slots}/{a.total_slots} free</span>
                <span>{a.active_bookings} active</span>
                <span>{money(a.revenue)}</span>
              </div>
            </Card>
          ))}
        </div>
      )}
    </div>
  );
}
