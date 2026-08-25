import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

const TypeBureauConfig = {
  0: 'Individuel',
  1: 'Open space',
  2: 'Salle de réunion',
};

const EtatActifConfig = {
  0: { label: 'Neuf', tone: 'success' },
  1: { label: 'Bon état', tone: 'success' },
  2: { label: 'À réparer', tone: 'warning' },
  3: { label: 'Hors service', tone: 'danger' },
};

const MonHistorique = () => {
  const [postes, setPostes] = useState([]);
  const [actifs, setActifs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const fetchHistory = async () => {
      setLoading(true);
      setError('');
      try {
        const response = await api.get('/agents/me/history');
        setPostes(response.data.postes || []);
        setActifs(response.data.actifs || []);
      } catch (err) {
        console.error('Failed to load my history:', err);
        setError('Impossible de charger votre historique.');
      } finally { setLoading(false); }
    };
    fetchHistory();
  }, []);

  return (
    <div>
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Mon historique' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Mon historique
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">L'ensemble de vos affectations de poste et d'actifs, passées et en cours.</p>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="th-label border-b border-border-subtle pb-2 mb-0">Bureaux</div>
      <div className="border border-border-subtle border-t-0 bg-surface-bg overflow-hidden overflow-x-auto mb-8">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Bureau</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Type</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Début</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Fin</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Motif</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement...</td></tr>
            ) : postes.length === 0 ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary italic">Aucune affectation de poste enregistrée.</td></tr>
            ) : (
              postes.map((p) => (
                <tr key={p.idAffectationPoste} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>N° {p.bureau.numero}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{TypeBureauConfig[p.bureau.type] ?? '—'}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(p.dateAffectation).toLocaleDateString('fr-FR')}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{p.dateFin ? new Date(p.dateFin).toLocaleDateString('fr-FR') : '—'}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{p.motif || '—'}</td>
                  <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={p.estActive ? 'success' : 'neutral'}>{p.estActive ? 'Active' : 'Terminée'}</StatusBadge></td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="th-label border-b border-border-subtle pb-2 mb-0">Actifs</div>
      <div className="border border-border-subtle border-t-0 bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Actif</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">État</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Début</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Fin</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement...</td></tr>
            ) : actifs.length === 0 ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary italic">Aucune affectation d'actif enregistrée.</td></tr>
            ) : (
              actifs.map((a) => {
                const et = EtatActifConfig[a.actif.etat] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={a.idAffectationActif} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-semibold text-text-primary">{a.actif.nom}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={et.tone}>{et.label}</StatusBadge></td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(a.dateAffectation).toLocaleDateString('fr-FR')}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{a.dateFin ? new Date(a.dateFin).toLocaleDateString('fr-FR') : '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={a.estActive ? 'success' : 'neutral'}>{a.estActive ? 'Active' : 'Terminée'}</StatusBadge></td>
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

export default MonHistorique;
