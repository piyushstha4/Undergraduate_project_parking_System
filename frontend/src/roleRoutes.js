// Central map of which URL prefixes belong to which role, so a post-login
// redirect can be validated against the role that actually just logged in
// (see Login.jsx) instead of blindly trusting a stale router "from" state
// left over from a previous session/role.
export const roleHome = { user: '/', parking_owner: '/owner', admin: '/admin' };

const roleAllowedPrefixes = {
  user: ['/', '/areas', '/bookings'],
  parking_owner: ['/owner'],
  admin: ['/admin'],
};

export function isPathAllowedForRole(pathname, role) {
  const prefixes = roleAllowedPrefixes[role] || [];
  return prefixes.some((p) => {
    if (p === '/') return pathname === '/';
    return pathname === p || pathname.startsWith(`${p}/`);
  });
}

export function resolvePostLoginDest(role, fromPathname) {
  if (fromPathname && isPathAllowedForRole(fromPathname, role)) return fromPathname;
  return roleHome[role] || '/';
}
