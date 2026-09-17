import { useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { roleHome } from '../roleRoutes';

const roleLinks = {
  user: [
    { to: '/', label: 'Find Parking' },
    { to: '/bookings', label: 'My Bookings' },
  ],
  parking_owner: [
    { to: '/owner', label: 'Dashboard' },
    { to: '/owner/areas', label: 'My Parking Areas' },
    { to: '/owner/bookings', label: 'Bookings' },
    { to: '/owner/reports', label: 'Reports' },
  ],
  admin: [
    { to: '/admin', label: 'Dashboard' },
    { to: '/admin/users', label: 'Users' },
    { to: '/admin/parking-areas', label: 'Parking Areas' },
  ],
};

export default function Navbar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [open, setOpen] = useState(false);

  const links = user ? roleLinks[user.role] || [] : [];

  function handleLogout() {
    logout();
    setOpen(false);
    navigate('/login');
  }

  return (
    <nav className="sticky top-0 z-40 bg-brand-700 text-white shadow-md">
      <div className="max-w-6xl mx-auto px-4">
        <div className="flex items-center justify-between h-14">
          <Link to={user ? roleHome[user.role] : '/'} className="flex items-center gap-2 font-semibold text-lg">
            <span aria-hidden="true">🅿️</span>
            <span>Smart Parking</span>
          </Link>

          <div className="hidden md:flex items-center gap-1">
            {links.map((l) => (
              <NavLink
                key={l.to}
                to={l.to}
                end={l.to === '/' || l.to === '/owner' || l.to === '/admin'}
                className={({ isActive }) =>
                  `px-3 py-2 rounded-md text-sm font-medium transition-colors ${
                    isActive ? 'bg-brand-800 text-white' : 'text-brand-100 hover:bg-brand-600'
                  }`
                }
              >
                {l.label}
              </NavLink>
            ))}
          </div>

          <div className="hidden md:flex items-center gap-3">
            {user ? (
              <>
                <span className="text-sm text-brand-100">
                  {user.name} <span className="opacity-70">({user.role.replace('_', ' ')})</span>
                </span>
                <button
                  onClick={handleLogout}
                  className="px-3 py-1.5 rounded-md text-sm font-medium bg-brand-900 hover:bg-black transition-colors"
                >
                  Log out
                </button>
              </>
            ) : (
              <>
                <Link to="/login" className="px-3 py-1.5 rounded-md text-sm font-medium hover:bg-brand-600">
                  Log in
                </Link>
                <Link to="/register" className="px-3 py-1.5 rounded-md text-sm font-medium bg-white text-brand-700 hover:bg-brand-50">
                  Sign up
                </Link>
              </>
            )}
          </div>

          <button
            className="md:hidden p-2 rounded-md hover:bg-brand-600"
            onClick={() => setOpen((o) => !o)}
            aria-label="Toggle menu"
          >
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              {open ? <path d="M6 6l12 12M18 6L6 18" /> : <path d="M4 7h16M4 12h16M4 17h16" />}
            </svg>
          </button>
        </div>
      </div>

      {open && (
        <div className="md:hidden border-t border-brand-600 bg-brand-700 px-4 py-3 space-y-1">
          {links.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.to === '/' || l.to === '/owner' || l.to === '/admin'}
              onClick={() => setOpen(false)}
              className={({ isActive }) =>
                `block px-3 py-2 rounded-md text-sm font-medium ${
                  isActive ? 'bg-brand-800' : 'text-brand-100 hover:bg-brand-600'
                }`
              }
            >
              {l.label}
            </NavLink>
          ))}
          {user ? (
            <>
              <div className="px-3 py-2 text-sm text-brand-100 border-t border-brand-600 mt-2 pt-2">
                {user.name} ({user.role.replace('_', ' ')})
              </div>
              <button
                onClick={handleLogout}
                className="w-full text-left px-3 py-2 rounded-md text-sm font-medium bg-brand-900"
              >
                Log out
              </button>
            </>
          ) : (
            <div className="flex gap-2 pt-2 border-t border-brand-600 mt-2">
              <Link to="/login" onClick={() => setOpen(false)} className="flex-1 text-center px-3 py-2 rounded-md text-sm font-medium bg-brand-600">
                Log in
              </Link>
              <Link to="/register" onClick={() => setOpen(false)} className="flex-1 text-center px-3 py-2 rounded-md text-sm font-medium bg-white text-brand-700">
                Sign up
              </Link>
            </div>
          )}
        </div>
      )}
    </nav>
  );
}
