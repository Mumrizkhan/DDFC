import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAppSelector } from '../store/hooks';

interface Props {
  children: React.ReactNode;
  requireRole?: 'Admin' | 'Staff' | 'Customer';
}

export const ProtectedRoute: React.FC<Props> = ({ children, requireRole }) => {
  const location = useLocation();
  const { staffToken, customerToken, staffUser, customer } = useAppSelector((s) => s.auth);

  if (requireRole === 'Customer') {
    if (!customerToken || !customer) {
      return <Navigate to="/portal/login" state={{ from: location }} replace />;
    }
    return <>{children}</>;
  }

  // Staff / Admin routes
  if (!staffToken || !staffUser) {
    return <Navigate to="/staff/login" state={{ from: location }} replace />;
  }

  if (requireRole === 'Admin' && staffUser.roleName !== 'Admin') {
    return <Navigate to="/backoffice" replace />;
  }

  return <>{children}</>;
};
