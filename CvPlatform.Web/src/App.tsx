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
import { PositionEditorPage } from './pages/PositionEditorPage';
import { PublicProfilePage } from './pages/PublicProfilePage';
import { UsersPage } from './pages/UsersPage';

function StaffOnly({ children, admin = false }: { children: React.ReactNode; admin?: boolean }) {
  const { isStaff, isAdmin, loading } = useAuth();
  if (loading) return null;
  return (admin ? isAdmin : isStaff) ? <>{children}</> : <Navigate to="/" replace />;
}

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
        <Route path="/positions/new" element={<StaffOnly><PositionEditorPage /></StaffOnly>} />
        <Route path="/positions/:id/edit" element={<StaffOnly><PositionEditorPage /></StaffOnly>} />
        <Route path="/positions/:id" element={<PositionDetailsPage />} />
        <Route path="/attributes" element={<Protected><AttributesPage /></Protected>} />
        <Route path="/users" element={<StaffOnly admin><UsersPage /></StaffOnly>} />
        <Route path="/profiles/:id" element={<Protected><PublicProfilePage /></Protected>} />
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
