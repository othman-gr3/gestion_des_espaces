import React from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import useAuth from '../hooks/useAuth';

const AFFECTATIONS_GROUP = {
  label: 'Affectations',
  items: [
    { name: 'Rechercher un bureau', path: '/rechercher-bureau' },
    { name: 'Affectations de poste', path: '/affectations-poste' },
    { name: "Affectations d'actifs", path: '/affectations-actif' },
    { name: 'Historique des affectations', path: '/historique-affectations' },
  ],
};

const NAV_GROUPS = {
  Administrateur: {
    pinned: { name: 'Tableau de bord', path: '/' },
    groups: [
      {
        label: 'Référentiel',
        items: [
          { name: 'Sites', path: '/sites' },
          { name: 'Bâtiments', path: '/batiments' },
          { name: 'Bureaux', path: '/bureaux' },
          { name: 'Agents', path: '/agents' },
          { name: 'Actifs', path: '/actifs' },
        ],
      },
      AFFECTATIONS_GROUP,
    ],
  },
  Gestionnaire: {
    groups: [
      {
        label: 'Affectations',
        items: [...AFFECTATIONS_GROUP.items, { name: 'Rechercher un actif', path: '/actifs' }],
      },
    ],
  },
  Agent: {
    groups: [
      {
        label: 'Mon espace',
        items: [
          { name: 'Mon bureau', path: '/mon-bureau' },
          { name: 'Mes actifs', path: '/mes-actifs' },
        ],
      },
    ],
  },
};

const AppShell = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const roleNav = NAV_GROUPS[user?.role] || { groups: [] };
  const allItems = [
    ...(roleNav.pinned ? [roleNav.pinned] : []),
    ...roleNav.groups.flatMap((g) => g.items),
  ];
  const currentLabel = allItems.find((item) => item.path === location.pathname)?.name || 'GestionEspaces';

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const renderLink = (item) => {
    const isActive = location.pathname === item.path;
    return (
      <Link
        key={item.path}
        to={item.path}
        className={`flex items-center gap-0 px-0 py-0 text-[13px] font-medium transition-colors relative ${
          isActive
            ? 'bg-primary-dark/70 text-white border-l-[3px] border-accent'
            : 'text-white/75 hover:text-white hover:bg-white/8 border-l-[3px] border-transparent'
        }`}
      >
        <span className="px-5 py-2.5 block w-full">{item.name}</span>
      </Link>
    );
  };

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-neutral-bg">
      {/* Sidebar */}
      <aside
        className="flex w-64 flex-col bg-primary text-white border-t-4 border-accent flex-shrink-0"
        style={{ fontFamily: 'var(--font-display)' }}
      >
        {/* Brand */}
        <div className="px-6 pt-5 pb-4 border-b border-white/10">
          <div
            className="text-[10.5px] text-accent/80 tracking-[0.2em] uppercase mb-1"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            Portail {user?.role?.toLowerCase()}
          </div>
          <div className="text-[16px] font-bold text-white leading-tight tracking-tight">
            GestionEspaces
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 py-3 overflow-y-auto">
          {roleNav.pinned && <div className="mb-2">{renderLink(roleNav.pinned)}</div>}
          {roleNav.groups.map((g) => (
            <div key={g.label}>
              <div
                className="px-5 pt-2 pb-1.5 text-[10px] text-white/40 tracking-[0.15em] uppercase"
                style={{ fontFamily: 'var(--font-mono)' }}
              >
                {g.label}
              </div>
              {g.items.map(renderLink)}
            </div>
          ))}
        </nav>

        {/* User footer */}
        <div className="px-5 py-4 border-t border-white/10 bg-black/10">
          <div
            className="text-[9.5px] text-white/50 tracking-[0.15em] uppercase mb-1"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            Connecté en tant que
          </div>
          <div className="text-[13px] font-semibold text-white leading-snug truncate">
            {user?.name || user?.email}
          </div>
          <div
            className="mt-1 text-[10px] uppercase tracking-widest text-accent font-medium"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            {user?.role}
          </div>
          <button
            onClick={handleLogout}
            className="mt-3 text-[11.5px] text-white/55 hover:text-white transition-colors"
            style={{ fontFamily: 'var(--font-sans)' }}
          >
            Se déconnecter →
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Topbar */}
        <header className="flex h-12 items-center justify-between border-b-2 border-border-subtle bg-surface-bg px-8">
          <div className="flex items-center gap-3">
            <div className="w-[3px] h-4 bg-accent flex-shrink-0" />
            <h1
              className="text-[15px] font-bold text-text-primary tracking-tight"
              style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}
            >
              {currentLabel}
            </h1>
          </div>
          <div
            className="text-[11px] text-text-secondary tracking-wide"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            {new Date().toLocaleDateString('fr-FR', {
              weekday: 'long',
              day: 'numeric',
              month: 'long',
              year: 'numeric',
            })}
          </div>
        </header>

        {/* Page body */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AppShell;
