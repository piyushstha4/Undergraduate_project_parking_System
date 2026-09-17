import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Card, Spinner } from '../../components/ui';

export default function AdminAreas() {
  const [areas, setAreas] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    api
      .get('/admin/parking-areas')
      .then(({ data }) => setAreas(data.areas))
      .catch((err) => setError(apiErrorMessage(err)));
  }, []);

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">Parking Areas</h1>
      <p className="text-sm text-gray-500 mb-6">All facilities registered on the platform.</p>

      {error && (
        <div className="mb-4">
          <Alert type="error">{error}</Alert>
        </div>
      )}

      {areas === null ? (
        <div className="flex justify-center py-20 text-brand-600">
          <Spinner />
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {areas.map((a) => (
            <Card key={a.id} className="p-4">
              <div className="flex items-start justify-between">
                <div>
                  <h3 className="font-semibold text-gray-900">{a.name}</h3>
                  <p className="text-sm text-gray-500">{a.address}, {a.city}</p>
                </div>
                <span className="text-sm font-medium text-brand-700">Rs. {a.price_per_hour}/hr</span>
              </div>
              <p className="text-xs text-gray-400 mt-2">Owner: {a.owner_name} ({a.owner_email})</p>
              <p className="text-sm text-gray-600 mt-2">{a.available_slots}/{a.total_slots} slots available</p>
            </Card>
          ))}
          {areas.length === 0 && <p className="text-gray-400 text-sm">No parking areas registered yet.</p>}
        </div>
      )}
    </div>
  );
}
