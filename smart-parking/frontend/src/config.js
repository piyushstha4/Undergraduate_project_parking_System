// Every value here comes from the project .env file (Vite exposes only VITE_* names).

export const config = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || 'http://localhost:4000/api',
  appName: import.meta.env.VITE_APP_NAME || 'Smart Parking',
  appTagline:
    import.meta.env.VITE_APP_TAGLINE ||
    'Smart Parking Slot Booking and Management System',
  tokenStorageKey: import.meta.env.VITE_TOKEN_STORAGE_KEY || 'sp_token',
  userStorageKey: import.meta.env.VITE_USER_STORAGE_KEY || 'sp_user',
  currencyLabel: import.meta.env.VITE_CURRENCY_LABEL || 'Rs.',
  currencyCode: import.meta.env.VITE_CURRENCY_CODE || 'NPR',
  defaultPaymentMethod: import.meta.env.VITE_DEFAULT_PAYMENT_METHOD || 'esewa',
  paymentMethods: (import.meta.env.VITE_PAYMENT_METHODS || 'esewa,khalti,card')
    .split(',')
    .map((m) => m.trim())
    .filter(Boolean),
  bookingLeadMinutes: Number(import.meta.env.VITE_BOOKING_LEAD_MINUTES || 10),
  bookingDurationHours: Number(import.meta.env.VITE_BOOKING_DURATION_HOURS || 2),
};

const paymentLabels = {
  esewa: 'eSewa',
  khalti: 'Khalti',
  card: 'Card',
};

export function paymentLabel(method) {
  return paymentLabels[method] || method;
}

export function money(amount) {
  return `${config.currencyLabel} ${amount}`;
}
