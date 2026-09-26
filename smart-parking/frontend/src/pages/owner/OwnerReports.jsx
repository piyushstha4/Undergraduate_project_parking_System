import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Card, Spinner } from '../../components/ui';
import { money } from '../../config';

export default function OwnerReports() {
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
  const maxRevenue = Math.max(1, ...areas.map((a) => a.revenue));

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">Reports</h1>
      <p className="text-sm text-gray-500 mb-6">Usage and revenue by parking area.</p>

      <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-6">
        <Card className="p-4">
          <p className="text-sm text-gray-500">Total bookings</p>
          <p className="text-2xl font-semibold text-gray-900">{totals.totalBookings}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Active bookings</p>
          <p className="text-2xl font-semibold text-gray-900">{totals.activeBookings}</p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Slot utilization</p>
          <p className="text-2xl font-semibold text-gray-900">
            {totals.totalSlots ? Math.round(((totals.totalSlots - totals.availableSlots) / totals.totalSlots) * 100) : 0}%
          </p>
        </Card>
        <Card className="p-4">
          <p className="text-sm text-gray-500">Total revenue</p>
          <p className="text-2xl font-semibold text-gray-900">{money(totals.revenue)}</p>
        </Card>
      </div>

      <Card className="p-5">
        <h2 className="font-semibold text-gray-900 mb-4">Revenue by parking area</h2>
        <div className="space-y-4">
          {areas.map((a) => (
            <div key={a.id}>
              <div className="flex justify-between text-sm mb-1">
                <span className="font-medium text-gray-800">{a.name}</span>
                <span className="text-gray-500">{money(a.revenue)} · {a.total_bookings} bookings</span>
              </div>
              <div className="w-full bg-gray-100 rounded-full h-2">
                <div
                  className="bg-brand-500 h-2 rounded-full"
                  style={{ width: `${(a.revenue / maxRevenue) * 100}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
}
