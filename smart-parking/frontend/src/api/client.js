import axios from 'axios';
import { config } from '../config';

const api = axios.create({ baseURL: config.apiBaseUrl });

api.interceptors.request.use((configRequest) => {
  const token = localStorage.getItem(config.tokenStorageKey);
  if (token) configRequest.headers.Authorization = `Bearer ${token}`;
  return configRequest;
});

api.interceptors.response.use(
  (res) => res,
  (err) => {
    if (err.response && err.response.status === 401) {
      localStorage.removeItem(config.tokenStorageKey);
      localStorage.removeItem(config.userStorageKey);
      if (!window.location.pathname.startsWith('/login')) {
        window.location.href = '/login';
      }
    }
    return Promise.reject(err);
  }
);

export function apiErrorMessage(err) {
  return err?.response?.data?.error || 'Something went wrong. Please try again.';
}

export default api;
