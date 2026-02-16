/**
 * Role hierarchy for future role-based access.
 * Order: developer < senior_developer < manager < vp
 */
export const ROLES = Object.freeze({
  developer: 'developer',
  senior_developer: 'senior_developer',
  manager: 'manager',
  vp: 'vp',
  admin: 'admin',
});

export const ROLE_LIST = Object.values(ROLES);

/** Roles that can access the admin panel (all for now) */
export const ADMIN_ACCESS_ROLES = ROLE_LIST;

/** Minimum role for CREATE (senior_developer+) */
export const CREATE_MIN_ROLE = ROLES.senior_developer;

/** Minimum role for UPDATE (senior_developer+) */
export const UPDATE_MIN_ROLE = ROLES.senior_developer;

/** Minimum role for DELETE (manager+) */
export const DELETE_MIN_ROLE = ROLES.manager;

/** READ: all roles */
export const READ_MIN_ROLE = ROLES.developer;

const ROLE_ORDER = [ROLES.developer, ROLES.senior_developer, ROLES.manager, ROLES.vp];

export function roleLevel(role) {
  const idx = ROLE_ORDER.indexOf(role);
  return idx === -1 ? -1 : idx;
}

export function hasMinimumRole(userRole, requiredRole) {
  return roleLevel(userRole) >= roleLevel(requiredRole);
}

export function canCreate(userRole) {
  return hasMinimumRole(userRole, CREATE_MIN_ROLE);
}

export function canRead(userRole) {
  return hasMinimumRole(userRole, READ_MIN_ROLE);
}

export function canUpdate(userRole) {
  return hasMinimumRole(userRole, UPDATE_MIN_ROLE);
}

export function canDelete(userRole) {
  return hasMinimumRole(userRole, DELETE_MIN_ROLE);
}

export function canAccessAdmin(userRole) {
  return userRole && ADMIN_ACCESS_ROLES.includes(userRole);
}

export function canAddCustomCategory(userRole) {
  return hasMinimumRole(userRole, DELETE_MIN_ROLE); // manager+
}
