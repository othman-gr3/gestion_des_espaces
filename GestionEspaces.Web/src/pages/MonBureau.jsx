import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

const StatutConfig = {
  0: { label: 'Disponible', tone: 'success' },
  1: { label: 'Maintenance', tone: 'warning' },
  2: { label: 'Hors service', tone: 'danger' },
};

const MonBureau = () => {
  const [bureau, setBureau] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchOffice = async () => {
      setLoading(true);
      setError('');
      try {
        const response = await api.get('/agents/me/office');
        setBureau(response.status === 204 ? null : response.data);
      } catch (err) {
        console.error('Failed to load my office:', err);
        setError('Impossible de charger votre bureau.');
      } finally { setLoading(false); }
    };
    fetchOffice();
  }, []);

  return (
    <div className="max-w-2xl">
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Mon bureau' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Mon bureau
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">Bureau actuellement affecté à votre profil (lecture seule).</p>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {loading ? (
        <div className="text-[13px] text-text-secondary">Chargement...</div>
      ) : !bureau ? (
        <div className="struct-card px-6 py-8 text-center">
          <div className="text-[13px] text-text-secondary italic">Aucun bureau ne vous est actuellement affecté.</div>
        </div>
      ) : (
        <div className="struct-card p-6">
          <div className="flex items-start justify-between mb-5">
            <div>
              <div className="th-label mb-1">Bureau</div>
              <div className="text-2xl font-extrabold text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 800 }}>
                N° {bureau.numero}
              </div>
            </div>
            <StatusBadge tone={(StatutConfig[bureau.statut] || {}).tone || 'neutral'}>
              {(StatutConfig[bureau.statut] || {}).label || 'Inconnu'}
            </StatusBadge>
          </div>
          <div className="grid grid-cols-2 gap-5 border-t border-border-subtle pt-5">
            <div>
              <div className="field-label">Type d'espace</div>
              <div className="text-[13px] text-text-primary mt-1">{bureau.type || '—'}</div>
            </div>
            <div>
              <div className="field-label">Étage</div>
              <div className="text-[13px] text-text-primary mt-1">Étage {bureau.etage}</div>
            </div>
            <div>
              <div className="field-label">Capacité</div>
              <div className="text-[13px] text-text-primary mt-1">{bureau.capacite} poste{bureau.capacite > 1 ? 's' : ''}</div>
            </div>
            <div>
              <div className="field-label">Superficie</div>
              <div className="text-[13px] text-text-primary mt-1" style={{ fontFamily: 'var(--font-mono)' }}>{bureau.superficie} m²</div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default MonBureau;
