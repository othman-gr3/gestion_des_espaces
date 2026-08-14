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
    <div className="flex min-h-screen w-screen overflow-hidden">
      {/* Left panel — institutional identity */}
      <div
        className="hidden lg:flex flex-col justify-between w-5/12 bg-primary p-12 border-t-4 border-accent"
        style={{ fontFamily: 'var(--font-display)' }}
      >
        <div>
          <div
            className="text-[11px] text-accent/90 tracking-[0.22em] uppercase mb-2"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            Portail administratif
          </div>
          <div className="w-8 h-[2px] bg-accent mb-8" />
        </div>

        <div>
          <h1
            className="text-5xl font-extrabold text-white leading-tight mb-6"
            style={{ fontWeight: 800, lineHeight: 1.05 }}
          >
            Gestion<br />des<br />Espaces
          </h1>
          <p className="text-[15px] text-white/70 leading-relaxed max-w-xs">
            Administration des implantations, affectation des agents et planification des créneaux d'occupation.
          </p>
        </div>

        <div
          className="text-[11px] text-white/40 tracking-wide"
          style={{ fontFamily: 'var(--font-mono)' }}
        >
          <div>ACCÈS RÉSERVÉ AU PERSONNEL HABILITÉ</div>
          <div className="mt-1">Toute connexion est journalisée</div>
        </div>
      </div>

      {/* Right panel — form */}
      <div className="flex flex-1 flex-col justify-center bg-neutral-bg px-8 sm:px-16 lg:px-20">
        {/* Mobile header */}
        <div className="mb-10 lg:hidden">
          <div className="text-[11px] text-accent tracking-[0.22em] uppercase mb-1" style={{ fontFamily: 'var(--font-mono)' }}>
            Portail administratif
          </div>
          <h1 className="text-3xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
            GestionEspaces
          </h1>
        </div>

        <div className="w-full max-w-sm">
          <div className="mb-8">
            <div className="flex items-center gap-3 mb-1">
              <div className="w-[3px] h-5 bg-primary flex-shrink-0" />
              <h2 className="text-[22px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
                Connexion
              </h2>
            </div>
            <p className="text-[13px] text-text-secondary mt-1 ml-[18px]">
              Renseignez vos identifiants de session
            </p>
          </div>

          {error && (
            <div className="mb-6 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-5">
            <div>
              <label htmlFor="email" className="field-label">
                Adresse e-mail
              </label>
              <input
                id="email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                className="form-field"
                placeholder="gestionnaire@domaine.fr"
                required
              />
            </div>

            <div>
              <label htmlFor="password" className="field-label">
                Mot de passe
              </label>
              <input
                id="password"
                type="password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                className="form-field"
                placeholder="••••••••"
                required
              />
            </div>

            <div className="pt-2">
              <button
                type="submit"
                id="login-submit"
                disabled={loading}
                className="w-full bg-primary px-6 py-3 text-[13px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors focus:outline-none disabled:opacity-50"
                style={{ fontFamily: 'var(--font-mono)' }}
              >
                {loading ? 'Vérification...' : 'Ouvrir une session'}
              </button>
            </div>
          </form>

          <div
            className="mt-8 border-t border-border-subtle pt-5 text-[12px] text-text-secondary space-y-1.5"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            <div><span className="text-primary font-medium">Administrateur</span> — référentiel complet</div>
            <div><span className="text-text-primary font-medium">Gestionnaire</span> — affectations postes/actifs</div>
            <div><span className="text-text-primary font-medium">Agent</span> — consultation de ses propres données</div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Login;
