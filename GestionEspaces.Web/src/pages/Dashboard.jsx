import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Dashboard = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState({ sites: 0, agents: 0, reservations: 0 });

  useEffect(() => {
    const fetchStats = async () => {
      try {
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

  const StatCard = ({ value, label, tag }) => (
    <div className="struct-card p-6">
      <div className="th-label mb-3">{tag}</div>
      <div
        className="text-5xl font-extrabold text-primary leading-none mb-2"
        style={{ fontFamily: 'var(--font-display)', fontWeight: 800 }}
      >
        {value}
      </div>
      <div className="text-[13px] text-text-secondary">{label}</div>
    </div>
  );

  return (
    <div className="space-y-8 max-w-5xl">
      {/* Welcome */}
      <div className="border-b-2 border-border-subtle pb-6">
        <div className="th-label text-accent mb-2">
          {new Date().toLocaleDateString('fr-FR', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' })}
        </div>
        <div className="flex items-baseline gap-4 flex-wrap">
          <h2
            className="text-3xl font-bold text-text-primary"
            style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}
          >
            {user?.name || 'Administrateur'}
          </h2>
          <span
            className="text-[11px] text-primary border border-primary px-2 py-1 tracking-widest uppercase"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            {user?.role}
          </span>
        </div>
        <p className="mt-2 text-[14px] text-text-secondary max-w-xl leading-relaxed">
          Vue consolidée des implantations, effectifs et créneaux d'occupation gérés sur ce portail.
        </p>
      </div>

      {/* Stat cards */}
      <div className="grid grid-cols-1 gap-px sm:grid-cols-3 bg-border-subtle border border-border-subtle">
        <StatCard value={stats.sites}        label="Sites d'implantation"       tag="Sites gérés" />
        <StatCard value={stats.agents}       label="Agents référencés"          tag="Agents enregistrés" />
        <StatCard value={stats.reservations} label="Réservations enregistrées"  tag="Réservations actives" />
      </div>

      {/* Quick actions */}
      <div>
        <div className="th-label border-b border-border-subtle pb-2 mb-0">
          Accès rapide
        </div>
        <div className="divide-y divide-border-subtle border border-border-subtle">
          {[
            { href: '/sites',        title: 'Gérer les sites',              desc: 'Consulter, créer ou mettre à jour les implantations géographiques' },
            { href: '/spaces',       title: 'Bâtiments & bureaux',          desc: 'Gérer le parc de bureaux, leurs capacités et statuts opérationnels' },
            { href: '/agents',       title: 'Agents',                       desc: 'Fiche agents, affectations de postes et attribution de matériel' },
            { href: '/reservations', title: 'Réservations en attente',      desc: 'Valider ou rejeter les demandes de créneaux horaires déposées' },
          ].map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="flex items-center justify-between px-5 py-4 bg-surface-bg hover:bg-neutral-bg group transition-colors"
            >
              <div>
                <div
                  className="text-[14px] font-semibold text-text-primary group-hover:text-primary transition-colors"
                  style={{ fontFamily: 'var(--font-display)', fontWeight: 600 }}
                >
                  {item.title}
                </div>
                <div className="text-[13px] text-text-secondary mt-0.5 leading-relaxed">{item.desc}</div>
              </div>
              <span className="text-text-secondary group-hover:text-primary transition-colors text-lg ml-6 flex-shrink-0">→</span>
            </a>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
