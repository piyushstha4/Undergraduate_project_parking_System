import { useEffect, useState, useCallback } from 'react';
import { Link } from 'react-router-dom';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Card, Input, Spinner, EmptyState, StatusBadge } from '../../components/ui';
import { config, money } from '../../config';

export default function Search() {
  const [areas, setAreas] = useState(null);
  const [error, setError] = useState('');
  const [q, setQ] = useState('');
  const [city, setCity] = useState('');
  const [maxPrice, setMaxPrice] = useState('');
  const [vehicleType, setVehicleType] = useState('');
  const [onlyAvailable, setOnlyAvailable] = useState(false);
  const [sort, setSort] = useState('');

  const load = useCallback(() => {
    setError('');
    const params = {};
    if (q) params.q = q;
    if (city) params.city = city;
    if (maxPrice) params.maxPrice = maxPrice;
    if (vehicleType) params.vehicleType = vehicleType;
    if (onlyAvailable) params.onlyAvailable = 'true';
    if (sort) params.sort = sort;

    api
      .get('/parking-areas', { params })
      .then(({ data }) => setAreas(data.areas))
      .catch((err) => setError(apiErrorMessage(err)));
  }, [q, city, maxPrice, vehicleType, onlyAvailable, sort]);

  useEffect(() => {
    const t = setTimeout(load, 250); // dynamic results without page reload, debounced
    return () => clearTimeout(t);
  }, [load]);

  return (
    <div className="max-w-6xl mx-auto px-4 py-6">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Find a parking spot</h1>
        <p className="text-sm text-gray-500 mt-1">Search real-time availability across the city and book in advance.</p>
      </div>

      <Card className="p-4 mb-6">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-3">
          <Input placeholder="Search by name or address" value={q} onChange={(e) => setQ(e.target.value)} />
          <Input placeholder="City" value={city} onChange={(e) => setCity(e.target.value)} />
          <Input
            placeholder={`Max price / hr (${config.currencyLabel})`}
            type="number"
            min="0"
            value={maxPrice}
            onChange={(e) => setMaxPrice(e.target.value)}
          />
          <select
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            value={vehicleType}
            onChange={(e) => setVehicleType(e.target.value)}
          >
            <option value="">Any vehicle</option>
            <option value="car">Car</option>
            <option value="bike">Bike</option>
          </select>
          <select
            className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400"
            value={sort}
            onChange={(e) => setSort(e.target.value)}
          >
            <option value="">Sort: default</option>
            <option value="price_asc">Price: low to high</option>
            <option value="price_desc">Price: high to low</option>
            <option value="availability">Most availability</option>
          </select>
        </div>
        <label className="flex items-center gap-2 mt-3 text-sm text-gray-700">
          <input type="checkbox" checked={onlyAvailable} onChange={(e) => setOnlyAvailable(e.target.checked)} />
          Only show areas with available slots
        </label>
      </Card>

      {error && (
        <div className="mb-4">
          <Alert type="error">{error}</Alert>
        </div>
      )}

      {areas === null ? (
        <div className="flex justify-center py-20 text-brand-600">
          <Spinner />
        </div>
      ) : areas.length === 0 ? (
        <EmptyState title="No parking areas match your search" subtitle="Try widening your filters." />
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {areas.map((area) => (
            <Link key={area.id} to={`/areas/${area.id}`}>
              <Card className="p-4 h-full hover:shadow-md hover:border-brand-300 transition-all cursor-pointer">
                <div className="flex items-start justify-between gap-2">
                  <h3 className="font-semibold text-gray-900">{area.name}</h3>
                  <StatusBadge status={area.available_slots > 0 ? 'available' : 'occupied'} />
                </div>
                <p className="text-sm text-gray-500 mt-1">{area.address}, {area.city}</p>
                <div className="flex items-center justify-between mt-4">
                  <span className="text-brand-700 font-semibold">{money(area.price_per_hour)}/hr</span>
                  <span className="text-sm text-gray-600">
                    {area.available_slots}/{area.total_slots} slots free
                  </span>
                </div>
                <p className="text-xs text-gray-500 mt-2">Capacity: {area.capacity} vehicles</p>
                <p className="text-xs text-gray-400 mt-1">Open {area.opening_time} – {area.closing_time}</p>
              </Card>
            </Link>
          ))}
        </div>
      )}
    </div>
  );
}
