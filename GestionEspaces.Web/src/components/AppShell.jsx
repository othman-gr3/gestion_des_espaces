import React from 'react';
import { Outlet, Link, useLocation, useNavigate } from 'react-router-dom';
import useAuth from '../hooks/useAuth';

const AppShell = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();

  const menuItems = [
    { name: 'Tableau de bord', path: '/' },
    { name: 'Sites', path: '/sites' },
    { name: 'Bâtiments & Bureaux', path: '/spaces' },
    { name: 'Agents', path: '/agents' },
    { name: 'Actifs', path: '/assets' },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
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
            className="text-[11px] text-accent/80 tracking-[0.2em] uppercase mb-1"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            Portail admin
          </div>
          <div className="text-[17px] font-bold text-white leading-tight tracking-tight">
            GestionEspaces
          </div>
        </div>

        {/* Navigation */}
        <nav className="flex-1 py-4 overflow-y-auto">
          {menuItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-0 px-0 py-0 text-[14px] font-medium transition-colors relative ${
                  isActive
                    ? 'bg-primary-dark/70 text-white border-l-[3px] border-accent'
                    : 'text-white/75 hover:text-white hover:bg-white/8 border-l-[3px] border-transparent'
                }`}
              >
                <span className="px-5 py-3 block w-full">{item.name}</span>
              </Link>
            );
          })}
        </nav>

        {/* User footer */}
        <div className="px-5 py-4 border-t border-white/10 bg-black/10">
          <div
            className="text-[10px] text-white/50 tracking-[0.15em] uppercase mb-1"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            Connecté en tant que
          </div>
          <div className="text-[14px] font-semibold text-white leading-snug truncate">
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
            className="mt-3 text-[12px] text-white/55 hover:text-white transition-colors"
            style={{ fontFamily: 'var(--font-sans)' }}
          >
            Se déconnecter →
          </button>
        </div>
      </aside>

      {/* Main content */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Topbar */}
        <header className="flex h-14 items-center justify-between border-b-2 border-border-subtle bg-surface-bg px-8">
          <div className="flex items-center gap-3">
            <div className="w-[3px] h-5 bg-accent flex-shrink-0" />
            <h1
              className="text-[17px] font-bold text-text-primary tracking-tight"
              style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}
            >
              {menuItems.find((item) => item.path === location.pathname)?.name || 'GestionEspaces'}
            </h1>
          </div>
          <div
            className="text-[12px] text-text-secondary tracking-wide"
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
        <main className="flex-1 overflow-y-auto p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AppShell;
