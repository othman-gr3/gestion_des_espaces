import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

const TypeConfig = {
  0: 'Changement de bureau',
  1: 'Problème avec mon bureau',
  2: 'Problème avec un actif',
  3: 'Autre',
};

const StatutConfig = {
  0: { label: 'En attente', tone: 'warning' },
  1: { label: 'En cours', tone: 'neutral' },
  2: { label: 'Résolue', tone: 'success' },
  3: { label: 'Rejetée', tone: 'danger' },
};

const MesDemandes = () => {
  const [demandes, setDemandes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [type, setType] = useState('0');
  const [description, setDescription] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState('');

  useEffect(() => { fetchDemandes(); }, []);

  const fetchDemandes = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/agents/me/demandes');
      setDemandes(response.data || []);
    } catch (err) {
      console.error('Failed to load my demandes:', err);
      setError('Impossible de charger vos demandes.');
    } finally { setLoading(false); }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    if (!description.trim()) { setFormError('Veuillez décrire votre demande.'); return; }
    setSubmitting(true);
    try {
      await api.post('/agents/me/demandes', { type: parseInt(type, 10), description });
      setDescription('');
      setType('0');
      fetchDemandes();
    } catch (err) {
      console.error('Demande creation error:', err);
      setFormError(err.response?.data?.detail || "L'envoi de la demande a échoué.");
    } finally { setSubmitting(false); }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Mes demandes' }]} />

      <div className="struct-card p-5 mb-6">
        <div className="th-label mb-4">Nouvelle demande</div>
        {formError && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-2.5 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleSubmit} className="space-y-4">
          <div>
            <label className="field-label">Type de demande</label>
            <select value={type} onChange={(e) => setType(e.target.value)} className="form-field">
              <option value="0">Changement de bureau</option>
              <option value="1">Problème avec mon bureau</option>
              <option value="2">Problème avec un actif</option>
              <option value="3">Autre</option>
            </select>
          </div>
          <div>
            <label className="field-label">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              className="form-field min-h-[80px] resize-none"
              placeholder="Décrivez votre demande..."
              maxLength={1000}
              rows={3}
            />
          </div>
          <div className="flex justify-end">
            <button
              type="submit"
              disabled={submitting}
              className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
              style={{ fontFamily: 'var(--font-mono)' }}
            >
              {submitting ? 'Envoi...' : 'Envoyer la demande'}
            </button>
          </div>
        </form>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="th-label border-b border-border-subtle pb-2 mb-0">Mes demandes</div>
      <div className="border border-border-subtle border-t-0 bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Type</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Description</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Envoyée le</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Réponse</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement...</td></tr>
            ) : demandes.length === 0 ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary italic">Aucune demande envoyée.</td></tr>
            ) : (
              demandes.map((d) => {
                const st = StatutConfig[d.statut] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={d.idDemande} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{TypeConfig[d.type] ?? '—'}</td>
                    <td className="px-4 py-2.5 text-[12.5px] text-text-secondary max-w-xs truncate" title={d.description}>{d.description}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(d.dateCreation).toLocaleDateString('fr-FR')}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={st.tone}>{st.label}</StatusBadge></td>
                    <td className="px-4 py-2.5 text-[12.5px] text-text-secondary max-w-xs truncate" title={d.reponse}>{d.reponse || '—'}</td>
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

export default MesDemandes;
