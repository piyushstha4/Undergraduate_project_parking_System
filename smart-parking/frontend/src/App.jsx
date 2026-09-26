import { BrowserRouter, Routes, Route, Outlet } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import Navbar from './components/Navbar';

import Login from './pages/auth/Login';
import Register from './pages/auth/Register';
import Search from './pages/user/Search';
import AreaDetail from './pages/user/AreaDetail';
import MyBookings from './pages/user/MyBookings';
import OwnerDashboard from './pages/owner/OwnerDashboard';
import ManageAreas from './pages/owner/ManageAreas';
import AreaSlots from './pages/owner/AreaSlots';
import OwnerBookings from './pages/owner/OwnerBookings';
import OwnerReports from './pages/owner/OwnerReports';
import AdminDashboard from './pages/admin/AdminDashboard';
import AdminUsers from './pages/admin/AdminUsers';
import AdminAreas from './pages/admin/AdminAreas';
import { config } from './config';

function Layout() {
  return (
    <div className="min-h-screen flex flex-col bg-gray-50">
      <Navbar />
      <main className="flex-1">
        <Outlet />
      </main>
      <footer className="text-center text-xs text-gray-400 py-6">
        {config.appTagline}
      </footer>
    </div>
  );
}

export default function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route element={<Layout />}>
            <Route path="/" element={<Search />} />
            <Route path="/areas/:id" element={<AreaDetail />} />
            <Route path="/login" element={<Login />} />
            <Route path="/register" element={<Register />} />

            <Route
              path="/bookings"
              element={
                <ProtectedRoute roles={['user', 'admin']}>
                  <MyBookings />
                </ProtectedRoute>
              }
            />

            <Route
              path="/owner"
              element={
                <ProtectedRoute roles={['parking_owner', 'admin']}>
                  <OwnerDashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/owner/areas"
              element={
                <ProtectedRoute roles={['parking_owner', 'admin']}>
                  <ManageAreas />
                </ProtectedRoute>
              }
            />
            <Route
              path="/owner/areas/:id/slots"
              element={
                <ProtectedRoute roles={['parking_owner', 'admin']}>
                  <AreaSlots />
                </ProtectedRoute>
              }
            />
            <Route
              path="/owner/bookings"
              element={
                <ProtectedRoute roles={['parking_owner', 'admin']}>
                  <OwnerBookings />
                </ProtectedRoute>
              }
            />
            <Route
              path="/owner/reports"
              element={
                <ProtectedRoute roles={['parking_owner', 'admin']}>
                  <OwnerReports />
                </ProtectedRoute>
              }
            />

            <Route
              path="/admin"
              element={
                <ProtectedRoute roles={['admin']}>
                  <AdminDashboard />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/users"
              element={
                <ProtectedRoute roles={['admin']}>
                  <AdminUsers />
                </ProtectedRoute>
              }
            />
            <Route
              path="/admin/parking-areas"
              element={
                <ProtectedRoute roles={['admin']}>
                  <AdminAreas />
                </ProtectedRoute>
              }
            />

            <Route path="*" element={<Search />} />
          </Route>
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}
