import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import KpiCard from '../components/KpiCard';

const Dashboard = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState({ sites: 0, batiments: 0, bureaux: 0, agents: 0, actifs: 0 });

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const [sitesRes, batimentsRes, bureauxRes, agentsRes, actifsRes] = await Promise.all([
          api.get('/sites?pageSize=1'),
          api.get('/batiments?pageSize=1'),
          api.get('/bureaux?pageSize=1'),
          api.get('/agents?pageSize=1'),
          api.get('/actifs?pageSize=1'),
        ]);
        setStats({
          sites: sitesRes.data.totalCount || 0,
          batiments: batimentsRes.data.totalCount || 0,
          bureaux: bureauxRes.data.totalCount || 0,
          agents: agentsRes.data.totalCount || 0,
          actifs: actifsRes.data.totalCount || 0,
        });
      } catch (error) {
        console.error('Error loading dashboard stats:', error);
      }
    };
    fetchStats();
  }, []);

  return (
    <div className="space-y-6 max-w-5xl">
      <Breadcrumb items={[{ label: 'Tableau de bord' }]} />

      {/* Header */}
      <div className="border-b-2 border-border-subtle pb-5 flex items-baseline gap-4 flex-wrap">
        <h2
          className="text-2xl font-bold text-text-primary"
          style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}
        >
          {user?.name || 'Administrateur'}
        </h2>
        <span
          className="text-[10.5px] text-primary border border-primary px-2 py-0.5 tracking-widest uppercase"
          style={{ fontFamily: 'var(--font-mono)' }}
        >
          {user?.role}
        </span>
      </div>

      {/* KPI row */}
      <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
        <KpiCard value={stats.sites} label="Sites d'implantation" tag="Sites" />
        <KpiCard value={stats.batiments} label="Bâtiments référencés" tag="Bâtiments" />
        <KpiCard value={stats.bureaux} label="Bureaux au référentiel" tag="Bureaux" />
        <KpiCard value={stats.agents} label="Agents enregistrés" tag="Agents" />
        <KpiCard value={stats.actifs} label="Actifs inventoriés" tag="Actifs" />
      </div>

      {/* Quick actions */}
      <div>
        <div className="th-label border-b border-border-subtle pb-2 mb-0">
          Référentiel
        </div>
        <div className="divide-y divide-border-subtle border border-border-subtle bg-surface-bg">
          {[
            { href: '/sites',     title: 'Sites',              desc: 'Consulter, créer ou mettre à jour les implantations géographiques' },
            { href: '/batiments', title: 'Bâtiments',           desc: 'Gérer les bâtiments et leurs caractéristiques par site' },
            { href: '/bureaux',   title: 'Bureaux',             desc: 'Gérer le parc de bureaux, leurs capacités et statuts opérationnels' },
            { href: '/agents',    title: 'Agents',               desc: 'Fiches agents : identité, coordonnées et fonction' },
            { href: '/actifs',    title: 'Actifs',               desc: "Suivre le parc d'équipements et leur état" },
          ].map((item) => (
            <a
              key={item.href}
              href={item.href}
              className="flex items-center justify-between px-4 py-3 hover:bg-neutral-bg group transition-colors"
            >
              <div>
                <div
                  className="text-[13.5px] font-semibold text-text-primary group-hover:text-primary transition-colors"
                  style={{ fontFamily: 'var(--font-display)', fontWeight: 600 }}
                >
                  {item.title}
                </div>
                <div className="text-[12px] text-text-secondary mt-0.5 leading-relaxed">{item.desc}</div>
              </div>
              <span className="text-text-secondary group-hover:text-primary transition-colors text-base ml-6 flex-shrink-0">→</span>
            </a>
          ))}
        </div>
      </div>
    </div>
  );
};

export default Dashboard;
