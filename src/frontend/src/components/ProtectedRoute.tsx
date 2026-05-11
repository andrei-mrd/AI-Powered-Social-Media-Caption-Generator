import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/useAuth';

interface Props {
  readonly children: React.ReactNode;
}

export default function ProtectedRoute({ children }: Readonly<Props>) {
  const { user, isLoading } = useAuth();

  if (isLoading) {
    return (
      <div className="full-page-loader">
        <div className="spinner" />
      </div>
    );
  }

  if (!user) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
