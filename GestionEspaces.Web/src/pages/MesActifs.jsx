import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

const EtatConfig = {
  0: { label: 'Neuf', tone: 'success' },
  1: { label: 'Bon état', tone: 'success' },
  2: { label: 'À réparer', tone: 'warning' },
  3: { label: 'Hors service', tone: 'danger' },
};

const MesActifs = () => {
  const [actifs, setActifs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchAssets = async () => {
      setLoading(true);
      setError('');
      try {
        const response = await api.get('/agents/me/assets');
        setActifs(response.data || []);
      } catch (err) {
        console.error('Failed to load my assets:', err);
        setError('Impossible de charger vos actifs.');
      } finally { setLoading(false); }
    };
    fetchAssets();
  }, []);

  return (
    <div>
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Mes actifs' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Mes actifs
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">Matériel actuellement confié à votre profil (lecture seule).</p>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              {['Désignation', 'Type', 'Marque / Modèle', 'N° de série', 'État'].map((h) => (
                <th key={h} className="px-4 py-2.5 text-left"><span className="th-label">{h}</span></th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement...</td></tr>
            ) : actifs.length === 0 ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary italic">Aucun actif ne vous est actuellement confié.</td></tr>
            ) : (
              actifs.map((a) => {
                const et = EtatConfig[a.etat] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={a.idActif} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-semibold text-text-primary">{a.nom}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{a.type}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{[a.marque, a.modele].filter(Boolean).join(' · ') || '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{a.numeroSerie || '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={et.tone}>{et.label}</StatusBadge></td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default MesActifs;
