import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import KpiCard from '../components/KpiCard';
import StatusBadge from '../components/StatusBadge';

const DEMANDE_TYPE_LABELS = {
  0: 'Changement de bureau',
  1: 'Problème avec un bureau',
  2: 'Problème avec un actif',
  3: 'Autre',
};

const AUDIT_EVENT_LABELS = {
  AgentAffecteAuBureauEvent: 'Affectation de poste créée',
  AffectationPosteClotureeEvent: 'Affectation de poste clôturée',
  BureauMisEnMaintenanceEvent: 'Bureau mis en maintenance',
  BureauRemisEnServiceEvent: 'Bureau remis en service',
  ActifAffecteEvent: "Affectation d'actif créée",
  AffectationActifClotureeEvent: "Affectation d'actif clôturée",
  DemandeCreeeEvent: 'Demande créée',
  DemandeResolueEvent: 'Demande résolue',
  DemandeRejeteeEvent: 'Demande rejetée',
};

// A single horizontal, segmented breakdown bar with a legend underneath —
// reused for both bureaux occupancy and actifs état so the two read the same way.
const BreakdownBar = ({ title, segments, total, emptyLabel }) => (
  <div className="struct-card p-5">
    <div className="th-label mb-3">{title}</div>
    {total === 0 ? (
      <div className="text-[12.5px] text-text-secondary py-2">{emptyLabel}</div>
    ) : (
      <>
        <div className="flex h-2.5 w-full overflow-hidden bg-neutral-bg">
          {segments.map((s) => (
            s.value > 0 && (
              <div key={s.label} className={`h-full ${s.barClass}`} style={{ width: `${(s.value / total) * 100}%` }} title={`${s.label}: ${s.value}`} />
            )
          ))}
        </div>
        <div className="mt-3 flex flex-wrap gap-x-5 gap-y-1.5">
          {segments.map((s) => (
            <div key={s.label} className="flex items-center gap-1.5 text-[12px] text-text-secondary">
              <span className={`inline-block h-2 w-2 ${s.barClass}`} />
              {s.label} <span className="font-semibold text-text-primary">{s.value}</span>
            </div>
          ))}
        </div>
      </>
    )}
  </div>
);

const Dashboard = () => {
  const { user } = useAuth();
  const [stats, setStats] = useState({ sites: 0, batiments: 0, bureaux: 0, agents: 0, actifs: 0 });
  const [bureauxParStatut, setBureauxParStatut] = useState({ disponible: 0, occupe: 0, maintenance: 0 });
  const [actifsParEtat, setActifsParEtat] = useState({ neuf: 0, bon: 0, reparer: 0, horsService: 0 });
  const [demandesEnAttente, setDemandesEnAttente] = useState({ total: 0, items: [] });
  const [activiteRecente, setActiviteRecente] = useState([]);

  useEffect(() => {
    const fetchStats = async () => {
      try {
        const [
          sitesRes, batimentsRes, bureauxRes, agentsRes, actifsRes,
          bureauxDispoRes, bureauxOccupeRes, bureauxMaintenanceRes,
          actifsNeufRes, actifsBonRes, actifsReparerRes, actifsHorsServiceRes,
          demandesRes, auditRes,
        ] = await Promise.all([
          api.get('/sites?pageSize=1'),
          api.get('/batiments?pageSize=1'),
          api.get('/bureaux?pageSize=1'),
          api.get('/agents?pageSize=1'),
          api.get('/actifs?pageSize=1'),
          api.get('/bureaux?pageSize=1&statut=0'),
          api.get('/bureaux?pageSize=1&statut=1'),
          api.get('/bureaux?pageSize=1&statut=2'),
          api.get('/actifs?pageSize=1&etat=0'),
          api.get('/actifs?pageSize=1&etat=1'),
          api.get('/actifs?pageSize=1&etat=2'),
          api.get('/actifs?pageSize=1&etat=3'),
          api.get('/demandes?pageSize=5&statut=0'),
          api.get('/audit-log?pageSize=5'),
        ]);

        setStats({
          sites: sitesRes.data.totalCount || 0,
          batiments: batimentsRes.data.totalCount || 0,
          bureaux: bureauxRes.data.totalCount || 0,
          agents: agentsRes.data.totalCount || 0,
          actifs: actifsRes.data.totalCount || 0,
        });
        setBureauxParStatut({
          disponible: bureauxDispoRes.data.totalCount || 0,
          occupe: bureauxOccupeRes.data.totalCount || 0,
          maintenance: bureauxMaintenanceRes.data.totalCount || 0,
        });
        setActifsParEtat({
          neuf: actifsNeufRes.data.totalCount || 0,
          bon: actifsBonRes.data.totalCount || 0,
          reparer: actifsReparerRes.data.totalCount || 0,
          horsService: actifsHorsServiceRes.data.totalCount || 0,
        });
        setDemandesEnAttente({
          total: demandesRes.data.totalCount || 0,
          items: demandesRes.data.items || [],
        });
        setActiviteRecente(auditRes.data.items || []);
      } catch (error) {
        console.error('Error loading dashboard stats:', error);
      }
    };
    fetchStats();
  }, []);

  const bureauxTotal = bureauxParStatut.disponible + bureauxParStatut.occupe + bureauxParStatut.maintenance;
  const actifsTotal = actifsParEtat.neuf + actifsParEtat.bon + actifsParEtat.reparer + actifsParEtat.horsService;

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

      {/* Occupancy breakdowns */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <BreakdownBar
          title="Occupation des bureaux"
          total={bureauxTotal}
          emptyLabel="Aucun bureau référencé."
          segments={[
            { label: 'Disponible', value: bureauxParStatut.disponible, barClass: 'bg-success' },
            { label: 'Occupé', value: bureauxParStatut.occupe, barClass: 'bg-warning' },
            { label: 'En maintenance', value: bureauxParStatut.maintenance, barClass: 'bg-danger' },
          ]}
        />
        <BreakdownBar
          title="État des actifs"
          total={actifsTotal}
          emptyLabel="Aucun actif référencé."
          segments={[
            { label: 'Neuf', value: actifsParEtat.neuf, barClass: 'bg-primary' },
            { label: 'Bon état', value: actifsParEtat.bon, barClass: 'bg-success' },
            { label: 'À réparer', value: actifsParEtat.reparer, barClass: 'bg-warning' },
            { label: 'Hors service', value: actifsParEtat.horsService, barClass: 'bg-danger' },
          ]}
        />
      </div>

      {/* Demandes en attente + Activité récente */}
      <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
        <div>
          <div className="flex items-center justify-between border-b border-border-subtle pb-2 mb-0">
            <div className="th-label">Demandes en attente ({demandesEnAttente.total})</div>
            <Link to="/demandes" className="text-[11px] font-semibold text-primary hover:text-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
              Voir tout →
            </Link>
          </div>
          <div className="divide-y divide-border-subtle border border-border-subtle bg-surface-bg">
            {demandesEnAttente.items.length === 0 ? (
              <div className="px-4 py-6 text-center text-[12.5px] text-text-secondary">Aucune demande en attente.</div>
            ) : (
              demandesEnAttente.items.map((d) => (
                <div key={d.idDemande} className="px-4 py-2.5">
                  <div className="flex items-center justify-between gap-3">
                    <div className="text-[13px] font-semibold text-text-primary truncate">{d.agentNomComplet}</div>
                    <div className="text-[11px] text-text-secondary whitespace-nowrap" style={{ fontFamily: 'var(--font-mono)' }}>
                      {new Date(d.dateCreation).toLocaleDateString('fr-FR')}
                    </div>
                  </div>
                  <div className="text-[12px] text-text-secondary mt-0.5">{DEMANDE_TYPE_LABELS[d.type] ?? '—'}</div>
                </div>
              ))
            )}
          </div>
        </div>

        <div>
          <div className="flex items-center justify-between border-b border-border-subtle pb-2 mb-0">
            <div className="th-label">Activité récente</div>
            <Link to="/journal-audit" className="text-[11px] font-semibold text-primary hover:text-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
              Voir tout →
            </Link>
          </div>
          <div className="divide-y divide-border-subtle border border-border-subtle bg-surface-bg">
            {activiteRecente.length === 0 ? (
              <div className="px-4 py-6 text-center text-[12.5px] text-text-secondary">Aucune activité récente.</div>
            ) : (
              activiteRecente.map((entry) => (
                <div key={entry.idAuditLog} className="px-4 py-2.5">
                  <div className="flex items-center justify-between gap-3">
                    <div className="text-[13px] font-semibold text-text-primary truncate">
                      {AUDIT_EVENT_LABELS[entry.eventType] || entry.eventType}
                    </div>
                    <div className="text-[11px] text-text-secondary whitespace-nowrap" style={{ fontFamily: 'var(--font-mono)' }}>
                      {new Date(entry.occurredOnUtc).toLocaleDateString('fr-FR')}
                    </div>
                  </div>
                  <div className="text-[12px] text-text-secondary mt-0.5">
                    {entry.utilisateurEmail || <span className="italic">Système</span>}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
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
