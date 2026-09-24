import { Navigate, Route, Routes } from 'react-router-dom';
import { AppLayout } from './components/AppLayout';
import { useAuth } from './hooks/useAuth';
import { AttributesPage } from './pages/AttributesPage';
import { CvDetailsPage } from './pages/CvDetailsPage';
import { CvsPage } from './pages/CvsPage';
import { LoginPage } from './pages/LoginPage';
import { PositionDetailsPage } from './pages/PositionDetailsPage';
import { PositionsPage } from './pages/PositionsPage';
import { ProfilePage } from './pages/ProfilePage';
import { OAuthCallbackPage, RegisterPage } from './pages/RegisterPage';
import { DashboardPage } from './pages/DashboardPage';
import { SearchPage } from './pages/SearchPage';

function Protected({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, loading } = useAuth();

  if (loading) {
    return null;
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
}

export function App() {
  return (
    <AppLayout>
      <Routes>
        <Route path="/" element={<DashboardPage />} />
        <Route path="/search" element={<SearchPage />} />
        <Route path="/positions" element={<PositionsPage />} />
        <Route path="/positions/:id" element={<PositionDetailsPage />} />
        <Route path="/attributes" element={<AttributesPage />} />
        <Route path="/login" element={<LoginPage />} />
        <Route path="/register" element={<RegisterPage />} />
        <Route path="/oauth-callback" element={<OAuthCallbackPage />} />
        <Route path="/profile" element={<Protected><ProfilePage /></Protected>} />
        <Route path="/profiles/:id/edit" element={<Protected><ProfilePage /></Protected>} />
        <Route path="/cvs" element={<Protected><CvsPage /></Protected>} />
        <Route path="/cvs/:id" element={<Protected><CvDetailsPage /></Protected>} />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Routes>
    </AppLayout>
  );
}
