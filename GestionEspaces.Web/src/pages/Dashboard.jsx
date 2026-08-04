import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Dashboard = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState({
    sites: 0,
    agents: 0,
    reservations: 0,
  });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        // Fetch sites, agents, and reservations to display quick count cards
        const [sitesRes, agentsRes, reservationsRes] = await Promise.all([
          api.get('/sites?pageSize=1'),
          api.get('/agents?pageSize=1'),
          api.get('/reservations?pageSize=1'),
        ]);

        setStats({
          sites: sitesRes.data.totalCount || 0,
          agents: agentsRes.data.totalCount || 0,
          reservations: reservationsRes.data.totalCount || 0,
        });
      } catch (error) {
        console.error('Error loading dashboard stats:', error);
      }
    };

    fetchStats();
  }, []);

  return (
    <div className="space-y-8">
      {/* Welcome banner */}
      <div className="border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-sm">
        <h2 className="text-xl font-bold text-text-primary">Bonjour, {user?.name || 'Administrateur'}</h2>
        <p className="mt-1 text-sm text-text-secondary">
          Bienvenue sur le portail d'administration de GestionEspaces. Vous êtes connecté avec le rôle{' '}
          <span className="font-bold text-primary">{user?.role}</span>.
        </p>
      </div>

      {/* Metric Cards Grid */}
      <div className="grid grid-cols-1 gap-6 sm:grid-cols-3">
        <div className="border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-sm">
          <div className="text-xs font-bold uppercase tracking-wider text-text-secondary">Sites gérés</div>
          <div className="mt-2 text-3xl font-bold text-primary">{stats.sites}</div>
        </div>

        <div className="border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-sm">
          <div className="text-xs font-bold uppercase tracking-wider text-text-secondary">Agents enregistrés</div>
          <div className="mt-2 text-3xl font-bold text-primary">{stats.agents}</div>
        </div>

        <div className="border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-sm">
          <div className="text-xs font-bold uppercase tracking-wider text-text-secondary">Réservations actives</div>
          <div className="mt-2 text-3xl font-bold text-primary">{stats.reservations}</div>
        </div>
      </div>

      {/* Quick shortcuts */}
      <div className="border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-sm">
        <h3 className="text-sm font-bold uppercase tracking-wider text-text-secondary mb-4">Raccourcis d'administration</h3>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
          <a
            href="/sites"
            className="flex items-center justify-between p-4 border border-border-subtle rounded hover:bg-neutral-bg transition-colors"
          >
            <div>
              <div className="text-sm font-bold text-text-primary">Gérer les sites</div>
              <div className="text-xs text-text-secondary">Consulter, ajouter ou modifier les implantations</div>
            </div>
            <span className="text-primary font-bold">→</span>
          </a>

          <a
            href="/reservations"
            className="flex items-center justify-between p-4 border border-border-subtle rounded hover:bg-neutral-bg transition-colors"
          >
            <div>
              <div className="text-sm font-bold text-text-primary">Gérer les réservations</div>
              <div className="text-xs text-text-secondary">Consulter et valider les créneaux horaires des salles</div>
            </div>
            <span className="text-primary font-bold">→</span>
          </a>
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
