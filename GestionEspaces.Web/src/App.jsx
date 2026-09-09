import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider } from './context/AuthContext';
import ProtectedRoute from './components/ProtectedRoute';
import AppShell from './components/AppShell';
import useAuth from './hooks/useAuth';

import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Sites from './pages/Sites';
import Batiments from './pages/Batiments';
import Bureaux from './pages/Bureaux';
import Agents from './pages/Agents';
import Assets from './pages/Assets';
import RechercheBureaux from './pages/RechercheBureaux';
import AffectationsPoste from './pages/AffectationsPoste';
import AffectationsActif from './pages/AffectationsActif';
import HistoriqueAffectations from './pages/HistoriqueAffectations';
import MonBureau from './pages/MonBureau';
import MesActifs from './pages/MesActifs';
import MonHistorique from './pages/MonHistorique';
import MonProfil from './pages/MonProfil';
import MonCompte from './pages/MonCompte';
import MesDemandes from './pages/MesDemandes';
import Demandes from './pages/Demandes';
import JournalAudit from './pages/JournalAudit';
import RechercheIA from './pages/RechercheIA';
import Utilisateurs from './pages/Utilisateurs';

// Landing page at "/" — each role has a different home, since only
// Administrateur can read the referentiel endpoints the Dashboard calls.
const RoleLanding = () => {
  const { user } = useAuth();
  if (user?.role === 'Gestionnaire') return <Navigate to="/rechercher-bureau" replace />;
  if (user?.role === 'Agent') return <Navigate to="/mon-bureau" replace />;
  return <Dashboard />;
};

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
            <Route index element={<RoleLanding />} />

            {/* Administrateur — référentiel */}
            <Route path="sites" element={<ProtectedRoute requiredRole="Administrateur"><Sites /></ProtectedRoute>} />
            <Route path="batiments" element={<ProtectedRoute requiredRole="Administrateur"><Batiments /></ProtectedRoute>} />
            <Route path="bureaux" element={<ProtectedRoute requiredRole="Administrateur"><Bureaux /></ProtectedRoute>} />
            <Route path="agents" element={<ProtectedRoute requiredRole="Administrateur"><Agents /></ProtectedRoute>} />
            <Route path="actifs" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><Assets /></ProtectedRoute>} />
            <Route path="journal-audit" element={<ProtectedRoute requiredRole="Administrateur"><JournalAudit /></ProtectedRoute>} />
            <Route path="utilisateurs" element={<ProtectedRoute requiredRole="Administrateur"><Utilisateurs /></ProtectedRoute>} />

            {/* Administrateur + Gestionnaire — affectations (matches the backend's GestionAffectations/ReferentielLecture policies) */}
            <Route path="rechercher-bureau" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><RechercheBureaux /></ProtectedRoute>} />
            <Route path="affectations-poste" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><AffectationsPoste /></ProtectedRoute>} />
            <Route path="affectations-actif" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><AffectationsActif /></ProtectedRoute>} />
            <Route path="historique-affectations" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><HistoriqueAffectations /></ProtectedRoute>} />
            <Route path="recherche-ia" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><RechercheIA /></ProtectedRoute>} />
            <Route path="demandes" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><Demandes /></ProtectedRoute>} />

            {/* Administrateur + Gestionnaire — self-service account (Agent has its own richer "Mon profil" below) */}
            <Route path="mon-compte" element={<ProtectedRoute requiredRole={['Administrateur', 'Gestionnaire']}><MonCompte /></ProtectedRoute>} />

            {/* Agent — mon espace */}
            <Route path="mon-bureau" element={<ProtectedRoute requiredRole="Agent"><MonBureau /></ProtectedRoute>} />
            <Route path="mes-actifs" element={<ProtectedRoute requiredRole="Agent"><MesActifs /></ProtectedRoute>} />
            <Route path="mon-historique" element={<ProtectedRoute requiredRole="Agent"><MonHistorique /></ProtectedRoute>} />
            <Route path="mon-profil" element={<ProtectedRoute requiredRole="Agent"><MonProfil /></ProtectedRoute>} />
            <Route path="mes-demandes" element={<ProtectedRoute requiredRole="Agent"><MesDemandes /></ProtectedRoute>} />
          </Route>
        </Routes>
      </Router>
    </AuthProvider>
  );
}

export default App;
