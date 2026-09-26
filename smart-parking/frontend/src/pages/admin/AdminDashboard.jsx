import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Card, Spinner, StatusBadge } from '../../components/ui';
import { money } from '../../config';

function StatCard({ label, value }) {
  return (
    <Card className="p-4">
      <p className="text-sm text-gray-500">{label}</p>
      <p className="text-2xl font-semibold text-gray-900 mt-1">{value}</p>
    </Card>
  );
}

export default function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get('/admin/stats')
      .then(({ data }) => setStats(data))
      .catch((err) => setError(apiErrorMessage(err)));
  }, []);

  if (error) {
    return (
      <div className="max-w-5xl mx-auto px-4 py-8">
        <Alert type="error">{error}</Alert>
      </div>
    );
  }
  if (!stats) {
    return (
      <div className="flex justify-center py-20 text-brand-600">
        <Spinner />
      </div>
    );
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">System Dashboard</h1>
      <p className="text-sm text-gray-500 mb-6">Monitor system-wide activity and usage.</p>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
        <StatCard label="Users" value={stats.totalUsers} />
        <StatCard label="Parking Owners" value={stats.totalOwners} />
        <StatCard label="Parking Areas" value={stats.totalAreas} />
        <StatCard label="Total Slots" value={stats.totalSlots} />
        <StatCard label="Available Slots" value={stats.availableSlots} />
        <StatCard label="Active Bookings" value={stats.activeBookings} />
        <StatCard label="Total Bookings" value={stats.totalBookings} />
        <StatCard label="Revenue" value={money(stats.revenue)} />
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card className="p-5">
          <h2 className="font-semibold text-gray-900 mb-3">Bookings by status</h2>
          <div className="space-y-2">
            {stats.bookingsByStatus.map((row) => (
              <div key={row.status} className="flex items-center justify-between text-sm">
                <StatusBadge status={row.status} />
                <span className="font-medium text-gray-700">{row.count}</span>
              </div>
            ))}
          </div>
        </Card>

        <Card className="p-5">
          <h2 className="font-semibold text-gray-900 mb-3">Recent bookings</h2>
          <div className="space-y-2">
            {stats.recentBookings.map((b) => (
              <div key={b.id} className="flex items-center justify-between text-sm border-b last:border-0 pb-2 last:pb-0">
                <div>
                  <p className="text-gray-800">{b.user_name} → {b.area_name}</p>
                  <p className="text-xs text-gray-400">{new Date(b.created_at).toLocaleString()}</p>
                </div>
                <StatusBadge status={b.status} />
              </div>
            ))}
            {stats.recentBookings.length === 0 && <p className="text-sm text-gray-400">No bookings yet.</p>}
          </div>
        </Card>
      </div>
    </div>
  );
}
