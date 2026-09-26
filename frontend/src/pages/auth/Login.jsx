import { useState } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import { apiErrorMessage } from '../../api/client';
import { Alert, Button, Input, Card } from '../../components/ui';
import { resolvePostLoginDest } from '../../roleRoutes';

export default function Login() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [form, setForm] = useState({ email: '', password: '' });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  async function handleSubmit(e) {
    e.preventDefault();
    setError('');
    setLoading(true);
    try {
      const user = await login(form.email, form.password);
      const dest = resolvePostLoginDest(user.role, location.state?.from?.pathname);
      navigate(dest, { replace: true });
    } catch (err) {
      setError(apiErrorMessage(err));
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="min-h-[80vh] flex items-center justify-center px-4 py-10">
      <Card className="w-full max-w-md p-6 sm:p-8">
        <h1 className="text-2xl font-semibold text-gray-900">Welcome back</h1>
        <p className="text-sm text-gray-500 mt-1 mb-6">Log in to book and manage your parking.</p>

        {error && (
          <div className="mb-4">
            <Alert type="error">{error}</Alert>
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <Input
            label="Email"
            type="email"
            required
            value={form.email}
            onChange={(e) => setForm({ ...form, email: e.target.value })}
            placeholder="you@example.com"
          />
          <Input
            label="Password"
            type="password"
            required
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
            placeholder="••••••••"
          />
          <Button type="submit" disabled={loading} className="w-full">
            {loading ? 'Logging in…' : 'Log in'}
          </Button>
        </form>

        <p className="text-sm text-gray-500 mt-6 text-center">
          Don&apos;t have an account?{' '}
          <Link to="/register" className="text-brand-600 font-medium hover:underline">
            Sign up
          </Link>
        </p>

        <div className="mt-6 border-t pt-4 text-xs text-gray-400">
          <p className="font-medium text-gray-500 mb-1">Demo accounts</p>
          <p>User: user@smartparking.com / User@123</p>
          <p>Parking Owner: owner1@smartparking.com / Owner@123</p>
          <p>Admin: admin@smartparking.com / Admin@123</p>
        </div>
      </Card>
    </div>
  );
}
