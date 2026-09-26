export function StatusBadge({ status }) {
  const styles = {
    available: 'bg-emerald-100 text-emerald-700',
    booked: 'bg-amber-100 text-amber-700',
    occupied: 'bg-amber-100 text-amber-700',
    maintenance: 'bg-gray-200 text-gray-600',
    confirmed: 'bg-emerald-100 text-emerald-700',
    pending_payment: 'bg-amber-100 text-amber-700',
    cancelled: 'bg-rose-100 text-rose-700',
    completed: 'bg-slate-200 text-slate-700',
    expired: 'bg-rose-100 text-rose-700',
    active: 'bg-emerald-100 text-emerald-700',
    suspended: 'bg-rose-100 text-rose-700',
  };
  const label = status.replace('_', ' ');
  return (
    <span className={`inline-block px-2 py-0.5 rounded-full text-xs font-medium capitalize ${styles[status] || 'bg-gray-100 text-gray-700'}`}>
      {label}
    </span>
  );
}

export function Alert({ type = 'error', children, onClose }) {
  const styles = {
    error: 'bg-rose-50 text-rose-700 border-rose-200',
    success: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    info: 'bg-blue-50 text-blue-700 border-blue-200',
  };
  return (
    <div className={`border rounded-lg px-4 py-3 text-sm flex items-start justify-between gap-3 ${styles[type]}`}>
      <span>{children}</span>
      {onClose && (
        <button onClick={onClose} className="opacity-60 hover:opacity-100" aria-label="Dismiss">
          ✕
        </button>
      )}
    </div>
  );
}

export function Spinner({ className = '' }) {
  return (
    <div className={`inline-block animate-spin rounded-full border-2 border-current border-t-transparent h-5 w-5 ${className}`} role="status" aria-label="Loading" />
  );
}

export function Card({ children, className = '' }) {
  return <div className={`bg-white rounded-xl border border-gray-200 shadow-sm ${className}`}>{children}</div>;
}

export function Button({ children, variant = 'primary', className = '', ...props }) {
  const variants = {
    primary: 'bg-brand-600 hover:bg-brand-700 text-white disabled:bg-gray-300',
    secondary: 'bg-white border border-gray-300 hover:bg-gray-50 text-gray-800',
    danger: 'bg-rose-600 hover:bg-rose-700 text-white disabled:bg-gray-300',
    ghost: 'hover:bg-gray-100 text-gray-700',
  };
  return (
    <button
      className={`px-4 py-2 rounded-lg text-sm font-medium transition-colors disabled:cursor-not-allowed ${variants[variant]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}

export function Input({ label, error, className = '', ...props }) {
  return (
    <label className="block">
      {label && <span className="block text-sm font-medium text-gray-700 mb-1">{label}</span>}
      <input
        className={`w-full rounded-lg border px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-400 ${
          error ? 'border-rose-400' : 'border-gray-300'
        } ${className}`}
        {...props}
      />
      {error && <span className="text-xs text-rose-600 mt-1 block">{error}</span>}
    </label>
  );
}

export function EmptyState({ title, subtitle, action }) {
  return (
    <div className="text-center py-16 px-4">
      <div className="text-4xl mb-3">🅿️</div>
      <h3 className="text-lg font-semibold text-gray-800">{title}</h3>
      {subtitle && <p className="text-sm text-gray-500 mt-1">{subtitle}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}
