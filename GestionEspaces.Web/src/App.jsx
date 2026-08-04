import React from 'react';
import { BrowserRouter as Router, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import AppShell from './components/AppShell';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Sites from './pages/Sites';
import Spaces from './pages/Spaces';
import Agents from './pages/Agents';
import Assets from './pages/Assets';
import Reservations from './pages/Reservations';

function App() {
  return (
    <AuthProvider>
      <Router>
        <Routes>
          <Route path="/login" element={<Login />} />
          
          <Route
            path="/"
            element={
              <ProtectedRoute>
                <AppShell />
              </ProtectedRoute>
            }
          >
            <Route index element={<Dashboard />} />
            <Route path="sites" element={<Sites />} />
            <Route path="spaces" element={<Spaces />} />
            <Route path="agents" element={<Agents />} />
            <Route path="assets" element={<Assets />} />
            <Route path="reservations" element={<Reservations />} />
          </Route>
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
