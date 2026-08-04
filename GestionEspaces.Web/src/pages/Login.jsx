import React, { useState } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import useAuth from '../hooks/useAuth';

const Login = () => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const from = location.state?.from?.pathname || '/';

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    if (!email || !password) {
      setError('Veuillez renseigner tous les champs.');
      setLoading(false);
      return;
    }

    const result = await login(email, password);
    if (result.success) {
      navigate(from, { replace: true });
    } else {
      setError(result.error || 'Identifiants invalides.');
    }
    setLoading(false);
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-neutral-bg px-4">
      <div className="w-full max-w-md border border-border-subtle bg-surface-bg p-8 shadow-sm rounded-lg">
        {/* Institutional Branding Header */}
        <div className="mb-8 text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-primary/10 text-primary">
            <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-5 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />
            </svg>
          </div>
          <h2 className="mt-4 text-2xl font-bold tracking-tight text-text-primary">Gestion des Espaces</h2>
          <p className="mt-2 text-sm text-text-secondary">Système d'administration et d'affectation</p>
        </div>

        {error && (
          <div className="mb-6 rounded bg-danger/10 border border-danger/20 p-3 text-sm font-semibold text-danger">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-6">
          <div>
            <label htmlFor="email" className="block text-xs font-bold uppercase tracking-wider text-text-secondary">
              Adresse E-mail
            </label>
            <input
              id="email"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              className="mt-2 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary placeholder-gray-400 focus:border-primary focus:bg-white focus:outline-none"
              placeholder="ex: gestionnaire@domain.com"
              required
            />
          </div>

          <div>
            <label htmlFor="password" className="block text-xs font-bold uppercase tracking-wider text-text-secondary">
              Mot de passe
            </label>
            <input
              id="password"
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              className="mt-2 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary placeholder-gray-400 focus:border-primary focus:bg-white focus:outline-none"
              placeholder="••••••••"
              required
            />
          </div>

          <button
            type="submit"
            disabled={loading}
            className="w-full rounded bg-primary py-2.5 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none disabled:opacity-50"
          >
            {loading ? 'Connexion...' : 'Se connecter'}
          </button>
        </form>

        <div className="mt-8 text-center text-xs text-text-secondary">
          <p>Pour le rôle <span className="font-bold">Gestionnaire</span>, utilisez une adresse contenant "gestion".</p>
          <p className="mt-1">Pour le rôle <span className="font-bold">Lecteur</span>, utilisez n'importe quelle autre adresse.</p>
        </div>
      </div>
    </div>
  );
};

export default Login;
