import { createContext, useContext, useEffect, useState, useCallback } from 'react';
import api from '../api/client';
import { config } from '../config';

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [user, setUser] = useState(() => {
    const raw = localStorage.getItem(config.userStorageKey);
    return raw ? JSON.parse(raw) : null;
  });
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const token = localStorage.getItem(config.tokenStorageKey);
    if (!token) {
      setReady(true);
      return;
    }
    api
      .get('/auth/me')
      .then(({ data }) => {
        setUser(data.user);
        localStorage.setItem(config.userStorageKey, JSON.stringify(data.user));
      })
      .catch(() => {
        localStorage.removeItem(config.tokenStorageKey);
        localStorage.removeItem(config.userStorageKey);
        setUser(null);
      })
      .finally(() => setReady(true));
  }, []);

  const login = useCallback(async (email, password) => {
    const { data } = await api.post('/auth/login', { email, password });
    localStorage.setItem(config.tokenStorageKey, data.token);
    localStorage.setItem(config.userStorageKey, JSON.stringify(data.user));
    setUser(data.user);
    return data.user;
  }, []);

  const register = useCallback(async (payload) => {
    await api.post('/auth/register', payload);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem(config.tokenStorageKey);
    localStorage.removeItem(config.userStorageKey);
    setUser(null);
  }, []);

  return (
    <AuthContext.Provider value={{ user, ready, login, register, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
