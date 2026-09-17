import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, Spinner, StatusBadge } from '../../components/ui';

export default function AdminUsers() {
  const [users, setUsers] = useState(null);
  const [error, setError] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [busyId, setBusyId] = useState(null);

  function load() {
    const params = roleFilter ? { role: roleFilter } : {};
    api
      .get('/admin/users', { params })
      .then(({ data }) => setUsers(data.users))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, [roleFilter]);

  async function toggleStatus(user) {
    setBusyId(user.id);
    setError('');
    const next = user.status === 'active' ? 'suspended' : 'active';
    try {
      await api.put(`/admin/users/${user.id}/status`, { status: next });
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="max-w-5xl mx-auto px-4 py-6">
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">Users</h1>
      <p className="text-sm text-gray-500 mb-6">Manage drivers and parking owners.</p>

      <div className="mb-4 flex gap-2">
        {[
          { value: '', label: 'All' },
          { value: 'user', label: 'Drivers' },
          { value: 'parking_owner', label: 'Parking Owners' },
        ].map((f) => (
          <button
            key={f.value}
            onClick={() => setRoleFilter(f.value)}
            className={`px-3 py-1.5 rounded-lg text-sm font-medium border ${
              roleFilter === f.value ? 'bg-brand-600 text-white border-brand-600' : 'bg-white text-gray-700 border-gray-300'
            }`}
          >
            {f.label}
          </button>
        ))}
      </div>

      {error && (
        <div className="mb-4">
          <Alert type="error" onClose={() => setError('')}>{error}</Alert>
        </div>
      )}

      {users === null ? (
        <div className="flex justify-center py-20 text-brand-600">
          <Spinner />
        </div>
      ) : (
        <Card className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="bg-gray-50 text-gray-500 text-left">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Email</th>
                <th className="px-4 py-3 font-medium">Role</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium">Joined</th>
                <th className="px-4 py-3 font-medium"></th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {users.map((u) => (
                <tr key={u.id}>
                  <td className="px-4 py-3 text-gray-900">{u.name}</td>
                  <td className="px-4 py-3 text-gray-600">{u.email}</td>
                  <td className="px-4 py-3 text-gray-600 capitalize">{u.role.replace('_', ' ')}</td>
                  <td className="px-4 py-3"><StatusBadge status={u.status} /></td>
                  <td className="px-4 py-3 text-gray-400">{new Date(u.created_at).toLocaleDateString()}</td>
                  <td className="px-4 py-3">
                    <Button
                      variant={u.status === 'active' ? 'danger' : 'secondary'}
                      className="text-xs px-2 py-1"
                      disabled={busyId === u.id}
                      onClick={() => toggleStatus(u)}
                    >
                      {u.status === 'active' ? 'Suspend' : 'Reactivate'}
                    </Button>
                  </td>
                </tr>
              ))}
              {users.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-8 text-center text-gray-400">No users found.</td>
                </tr>
              )}
            </tbody>
          </table>
        </Card>
      )}
    </div>
  );
}
