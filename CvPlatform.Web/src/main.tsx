import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { createBrowserRouter, RouterProvider } from 'react-router-dom';
import 'bootstrap/dist/css/bootstrap.min.css';
import './i18n';
import './styles.css';
import { App } from './App';
import { AuthProvider } from './hooks/useAuth';

const router = createBrowserRouter([{ path: '*', element: <AuthProvider><App /></AuthProvider> }]);
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <RouterProvider router={router} />
  </StrictMode>,
);
