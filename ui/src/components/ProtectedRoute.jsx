import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export default function ProtectedRoute({ children, requireAdmin = false }) {
  const { auth } = useAuth();
  const location = useLocation();

  if (!auth) {
    // Preserve the intended URL so we can redirect back after login
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  if (requireAdmin && !auth.isAdmin) {
    return <Navigate to="/" replace />;
  }

  return children;
}
