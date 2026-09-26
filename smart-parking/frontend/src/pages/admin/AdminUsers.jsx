import { useEffect, useState } from 'react';
import api, { apiErrorMessage } from '../../api/client';
import { Alert, Button, Card, Input, Spinner, StatusBadge } from '../../components/ui';

const emptyOwner = { name: '', email: '', phone: '', password: '' };

export default function AdminUsers() {
  const [users, setUsers] = useState(null);
  const [error, setError] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [busyId, setBusyId] = useState(null);
  const [ownerForm, setOwnerForm] = useState(emptyOwner);
  const [creating, setCreating] = useState(false);
  const [created, setCreated] = useState(null);
  const [passwordFor, setPasswordFor] = useState(null);
  const [newPassword, setNewPassword] = useState('');

  function load() {
    const params = roleFilter ? { role: roleFilter } : {};
    api
      .get('/admin/users', { params })
      .then(({ data }) => setUsers(data.users))
      .catch((err) => setError(apiErrorMessage(err)));
  }

  useEffect(load, [roleFilter]);

  async function createOwner(e) {
    e.preventDefault();
    setCreating(true);
    setError('');
    setCreated(null);
    try {
      const { data } = await api.post('/admin/users', { ...ownerForm, role: 'parking_owner' });
      setCreated({ email: ownerForm.email, password: ownerForm.password, name: data.user.name });
      setOwnerForm(emptyOwner);
      load();
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  async function savePassword(user) {
    setBusyId(user.id);
    setError('');
    try {
      await api.put(`/admin/users/${user.id}/password`, { password: newPassword });
      setCreated({ email: user.email, password: newPassword, name: user.name });
      setPasswordFor(null);
      setNewPassword('');
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

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
      <h1 className="text-2xl font-semibold text-gray-900 mb-1">User management</h1>
      <p className="text-sm text-gray-500 mb-6">
        Create a parking owner and give them the email and password. Drivers register their own accounts.
      </p>

      <Card className="p-5 mb-6">
        <h2 className="font-semibold text-gray-900 mb-3">New parking owner</h2>
        {created && (
          <div className="mb-4">
            <Alert type="success" onClose={() => setCreated(null)}>
              Give {created.name} this login: {created.email} / {created.password}
            </Alert>
          </div>
        )}
        <form onSubmit={createOwner} className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          <Input label="Full name" required value={ownerForm.name} onChange={(e) => setOwnerForm({ ...ownerForm, name: e.target.value })} />
          <Input label="Email (username)" type="email" required value={ownerForm.email} onChange={(e) => setOwnerForm({ ...ownerForm, email: e.target.value })} />
          <Input label="Phone (optional)" value={ownerForm.phone} onChange={(e) => setOwnerForm({ ...ownerForm, phone: e.target.value })} />
          <Input label="Password" type="text" required minLength={6} value={ownerForm.password} onChange={(e) => setOwnerForm({ ...ownerForm, password: e.target.value })} />
          <div className="sm:col-span-2">
            <Button type="submit" disabled={creating}>{creating ? 'Creating…' : 'Create owner account'}</Button>
          </div>
        </form>
      </Card>

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
                    <div className="flex flex-wrap items-center gap-2 justify-end">
                      {u.role === 'parking_owner' && (
                        passwordFor === u.id ? (
                          <>
                            <input
                              className="rounded-lg border border-gray-300 px-2 py-1 text-sm w-32"
                              type="text"
                              minLength={6}
                              placeholder="New password"
                              value={newPassword}
                              onChange={(e) => setNewPassword(e.target.value)}
                            />
                            <Button className="text-xs px-2 py-1" disabled={busyId === u.id || newPassword.length < 6} onClick={() => savePassword(u)}>
                              Save
                            </Button>
                          </>
                        ) : (
                          <Button
                            variant="secondary"
                            className="text-xs px-2 py-1"
                            onClick={() => { setPasswordFor(u.id); setNewPassword(''); }}
                          >
                            Set password
                          </Button>
                        )
                      )}
                      <Button
                        variant={u.status === 'active' ? 'danger' : 'secondary'}
                        className="text-xs px-2 py-1"
                        disabled={busyId === u.id}
                        onClick={() => toggleStatus(u)}
                      >
                        {u.status === 'active' ? 'Suspend' : 'Reactivate'}
                      </Button>
                    </div>
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
