import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { canAccessAdmin } from '../constants/roles';

/**
 * Protects routes by role. If user doesn't have access, redirects to target (default /).
 * For future use: pass requiredRole or allowedRoles to restrict by action.
 */
function RoleGuard({ children, requiredRole, allowedRoles, redirectTo = '/' }) {
  const { user } = useAuth();
  const location = useLocation();

  const allowed = allowedRoles
    ? allowedRoles.includes(user?.role)
    : requiredRole
      ? canAccessAdmin(user?.role) // generic admin check when requiredRole is set
      : canAccessAdmin(user?.role);

  if (!user) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }
  if (!allowed) {
    return <Navigate to={redirectTo} state={{ from: location }} replace />;
  }

  return children;
}

export default RoleGuard;
