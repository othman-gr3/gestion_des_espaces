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
    { name: 'Réservations', path: '/reservations' },
  ];

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <div className="flex h-screen w-screen overflow-hidden bg-neutral-bg">
      {/* Sidebar */}
      <aside className="flex w-64 flex-col bg-primary text-white">
        {/* Brand Header */}
        <div className="flex h-16 items-center px-6 border-b border-primary-dark">
          <span className="text-lg font-bold tracking-wider">GESTION ESPACES</span>
        </div>

        {/* Navigation */}
        <nav className="flex-1 space-y-1 py-6">
          {menuItems.map((item) => {
            const isActive = location.pathname === item.path;
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center px-6 py-3 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-primary-dark border-l-4 border-accent text-accent'
                    : 'text-gray-300 hover:bg-primary-dark hover:text-white'
                }`}
              >
                {item.name}
              </Link>
            );
          })}
        </nav>

        {/* User Info footer */}
        <div className="p-4 border-t border-primary-dark bg-primary-dark/50 flex flex-col gap-2">
          <div className="flex flex-col">
            <span className="text-xs text-gray-400">Connecté en tant que</span>
            <span className="text-sm font-semibold truncate">{user?.name || user?.email}</span>
            <span className="mt-1 text-[10px] uppercase font-bold tracking-wider text-accent bg-accent/10 px-2 py-0.5 rounded self-start">
              {user?.role}
            </span>
          </div>
          <button
            onClick={handleLogout}
            className="mt-2 w-full text-left text-xs font-semibold text-gray-400 hover:text-white transition-colors"
          >
            Se déconnecter
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Topbar */}
        <header className="flex h-16 items-center justify-between border-b border-border-subtle bg-surface-bg px-8">
          <h1 className="text-lg font-bold text-text-primary">
            {menuItems.find((item) => item.path === location.pathname)?.name || 'GestionEspaces'}
          </h1>
          <div className="flex items-center gap-4">
            <span className="text-xs text-text-secondary">{new Date().toLocaleDateString('fr-FR', { weekday: 'long', year: 'numeric', month: 'long', day: 'numeric' })}</span>
          </div>
        </header>

        {/* Page Body Container */}
        <main className="flex-1 overflow-y-auto p-8">
          <Outlet />
        </main>
      </div>
    </div>
  );
};

export default AppShell;
