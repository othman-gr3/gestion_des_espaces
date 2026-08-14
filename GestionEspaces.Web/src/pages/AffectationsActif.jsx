import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  listAgents, listActifs,
  createAssetAssignment, closeAssetAssignment,
  fetchAllAssignments,
} from '../services/affectationService';
import Breadcrumb from '../components/Breadcrumb';

const todayInputValue = () => new Date().toISOString().slice(0, 10);

const EtatOptions = [
  { value: '', label: 'Ne pas modifier' },
  { value: '1', label: 'Bon état' },
  { value: '2', label: 'À réparer' },
  { value: '3', label: 'Hors service' },
];

const AffectationsActif = () => {
  const location = useLocation();
  const prefill = location.state || {};

  const [agents, setAgents] = useState([]);
  const [actifs, setActifs] = useState([]);
  const [active, setActive] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [formAgentId, setFormAgentId] = useState('');
  const [formActifId, setFormActifId] = useState(prefill.actifId ? String(prefill.actifId) : '');
  const [formDate, setFormDate] = useState(todayInputValue());
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const [closingId, setClosingId] = useState(null);
  const [etatRetour, setEtatRetour] = useState('');

  useEffect(() => { loadReferentiel(); loadActive(); }, []);

  const loadReferentiel = async () => {
    try {
      const [agentsRes, actifsRes] = await Promise.all([listAgents(), listActifs()]);
      setAgents(agentsRes.data.items || []);
      setActifs(actifsRes.data.items || []);
    } catch (err) { console.error('Failed to load referentiel:', err); }
  };

  const loadActive = async () => {
    setLoading(true);
    setError('');
    try {
      const { actifAffectations } = await fetchAllAssignments();
      setActive(actifAffectations.filter((a) => !a.dateFin).sort((a, b) => new Date(b.dateAffectation) - new Date(a.dateAffectation)));
    } catch (err) {
      console.error('Failed to load active assignments:', err);
      setError('Impossible de charger les affectations actives.');
    } finally { setLoading(false); }
  };

  const actifLabel = (idActif) => {
    const a = actifs.find((x) => x.idActif === idActif);
    return a ? `${a.nom}${a.numeroSerie ? ` · S/N ${a.numeroSerie}` : ''}` : `Actif ${idActif}`;
  };
  const agentLabel = (idAgent) => {
    const a = agents.find((x) => x.idAgent === idAgent);
    return a ? `${a.nom.toUpperCase()} ${a.prenom}` : `Agent ${idAgent}`;
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    setFormError('');
    if (!formAgentId || !formActifId) { setFormError('Sélectionnez un agent et un actif.'); return; }
    setSubmitting(true);
    try {
      await createAssetAssignment(formAgentId, parseInt(formActifId, 10), new Date(formDate).toISOString());
      setFormAgentId('');
      setFormActifId('');
      loadActive();
    } catch (err) {
      console.error('Assignment error:', err);
      setFormError(err.response?.data?.detail || "Impossible d'attribuer ce matériel.");
    } finally { setSubmitting(false); }
  };

  const handleClose = async (affectation) => {
    try {
      await closeAssetAssignment(
        affectation.agentId,
        affectation.idAffectationActif,
        new Date().toISOString(),
        closingId === affectation.idAffectationActif && etatRetour !== '' ? parseInt(etatRetour, 10) : null
      );
      setClosingId(null);
      setEtatRetour('');
      loadActive();
    } catch (err) {
      console.error('Failed to close assignment:', err);
      alert(err.response?.data?.detail || "Impossible de clôturer l'affectation.");
    }
  };

  const availableActifs = actifs.filter((a) => a.etat !== 3 || String(a.idActif) === formActifId);

  return (
    <div>
      <Breadcrumb items={[{ label: 'Affectations' }, { label: "Affectations d'actifs" }]} />

      {/* Create form */}
      <div className="struct-card p-5 mb-6">
        <div className="th-label mb-4">Nouvelle attribution de matériel</div>
        {formError && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-2.5 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleCreate} className="grid grid-cols-1 gap-4 sm:grid-cols-4 sm:items-end">
          <div>
            <label className="field-label">Agent</label>
            <select value={formAgentId} onChange={(e) => setFormAgentId(e.target.value)} className="form-field" required>
              <option value="">Sélectionner...</option>
              {agents.map((a) => <option key={a.idAgent} value={a.idAgent}>{a.nom.toUpperCase()} {a.prenom} · {a.matricule}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Actif disponible</label>
            <select value={formActifId} onChange={(e) => setFormActifId(e.target.value)} className="form-field" required>
              <option value="">Sélectionner...</option>
              {availableActifs.map((a) => <option key={a.idActif} value={a.idActif}>{a.nom}{a.numeroSerie ? ` · S/N ${a.numeroSerie}` : ''}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Date de début</label>
            <input type="date" value={formDate} onChange={(e) => setFormDate(e.target.value)} className="form-field" required />
          </div>
          <button type="submit" disabled={submitting} className="bg-primary px-5 py-2 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50" style={{ fontFamily: 'var(--font-mono)' }}>
            {submitting ? 'Attribution...' : 'Attribuer'}
          </button>
        </form>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Active assignments */}
      <div className="th-label border-b border-border-subtle pb-2 mb-0">Attributions actives</div>
      <div className="border border-border-subtle border-t-0 bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Agent</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Actif</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Date de début</span></th>
              <th className="px-4 py-2.5 text-right"><span className="th-label">Clôture</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des attributions actives...</td></tr>
            ) : active.length === 0 ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucune attribution active.</td></tr>
            ) : (
              active.map((a) => {
                const isClosing = closingId === a.idAffectationActif;
                return (
                  <tr key={a.idAffectationActif} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{a.agent.nom.toUpperCase()} {a.agent.prenom}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{actifLabel(a.actifId)}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(a.dateAffectation).toLocaleDateString('fr-FR')}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      {isClosing ? (
                        <div className="flex items-center justify-end gap-2">
                          <select value={etatRetour} onChange={(e) => setEtatRetour(e.target.value)} className="form-field !w-40 !py-1">
                            {EtatOptions.map((o) => <option key={o.value} value={o.value}>{o.label}</option>)}
                          </select>
                          <button onClick={() => handleClose(a)} className="btn-text-action btn-text-action-danger">Confirmer</button>
                          <button onClick={() => { setClosingId(null); setEtatRetour(''); }} className="btn-text-action btn-text-action-muted">Annuler</button>
                        </div>
                      ) : (
                        <button onClick={() => { setClosingId(a.idAffectationActif); setEtatRetour(''); }} className="btn-text-action btn-text-action-danger">Clôturer</button>
                      )}
                    </td>
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

export default AffectationsActif;
