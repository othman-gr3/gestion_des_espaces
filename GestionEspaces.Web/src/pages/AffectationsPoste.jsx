import React, { useEffect, useState } from 'react';
import { useLocation } from 'react-router-dom';
import {
  listAgents, listBureaux,
  createOfficeAssignment, closeOfficeAssignment,
  fetchAllAssignments,
} from '../services/affectationService';
import Breadcrumb from '../components/Breadcrumb';

const todayInputValue = () => new Date().toISOString().slice(0, 10);

const AffectationsPoste = () => {
  const location = useLocation();
  const prefill = location.state || {};

  const [agents, setAgents] = useState([]);
  const [bureaux, setBureaux] = useState([]);
  const [active, setActive] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [formAgentId, setFormAgentId] = useState('');
  const [formBureauId, setFormBureauId] = useState(prefill.bureauId ? String(prefill.bureauId) : '');
  const [formDate, setFormDate] = useState(todayInputValue());
  const [formError, setFormError] = useState('');
  const [submitting, setSubmitting] = useState(false);

  useEffect(() => { loadReferentiel(); loadActive(); }, []);

  const loadReferentiel = async () => {
    try {
      const [agentsRes, bureauxRes] = await Promise.all([listAgents(), listBureaux()]);
      setAgents(agentsRes.data.items || []);
      setBureaux(bureauxRes.data.items || []);
    } catch (err) { console.error('Failed to load referentiel:', err); }
  };

  const loadActive = async () => {
    setLoading(true);
    setError('');
    try {
      const { posteAffectations } = await fetchAllAssignments();
      setActive(posteAffectations.filter((a) => !a.dateFin).sort((a, b) => new Date(b.dateAffectation) - new Date(a.dateAffectation)));
    } catch (err) {
      console.error('Failed to load active assignments:', err);
      setError('Impossible de charger les affectations actives.');
    } finally { setLoading(false); }
  };

  const bureauLabel = (idBureau) => {
    const b = bureaux.find((x) => x.idBureau === idBureau);
    return b ? `N° ${b.numero} — ${b.type || ''}` : `Bureau ${idBureau}`;
  };
  const agentLabel = (idAgent) => {
    const a = agents.find((x) => x.idAgent === idAgent);
    return a ? `${a.nom.toUpperCase()} ${a.prenom}` : `Agent ${idAgent}`;
  };

  const handleCreate = async (e) => {
    e.preventDefault();
    setFormError('');
    if (!formAgentId || !formBureauId) { setFormError('Sélectionnez un agent et un bureau.'); return; }
    setSubmitting(true);
    try {
      await createOfficeAssignment(formAgentId, parseInt(formBureauId, 10), new Date(formDate).toISOString());
      setFormAgentId('');
      setFormBureauId('');
      loadActive();
    } catch (err) {
      console.error('Assignment error:', err);
      setFormError(err.response?.data?.detail || "Impossible de créer l'affectation.");
    } finally { setSubmitting(false); }
  };

  const handleClose = async (affectation) => {
    if (!window.confirm(`Clôturer l'affectation de ${agentLabel(affectation.agentId)} au bureau ${bureauLabel(affectation.bureauId)} ?`)) return;
    try {
      await closeOfficeAssignment(affectation.agentId, affectation.idAffectationPoste, new Date().toISOString());
      loadActive();
    } catch (err) {
      console.error('Failed to close assignment:', err);
      alert(err.response?.data?.detail || "Impossible de clôturer l'affectation.");
    }
  };

  const availableBureaux = bureaux.filter((b) => b.statut === 0 || String(b.idBureau) === formBureauId);

  return (
    <div>
      <Breadcrumb items={[{ label: 'Affectations' }, { label: 'Affectations de poste' }]} />

      {/* Create form */}
      <div className="struct-card p-5 mb-6">
        <div className="th-label mb-4">Nouvelle affectation de poste</div>
        {prefill.bureauNumero && (
          <div className="mb-4 border-l-[3px] border-accent bg-accent/5 px-4 py-2.5 text-[12.5px] text-text-primary">
            Bureau présélectionné depuis la recherche : <span className="font-semibold">N° {prefill.bureauNumero}</span>
          </div>
        )}
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
            <label className="field-label">Bureau disponible</label>
            <select value={formBureauId} onChange={(e) => setFormBureauId(e.target.value)} className="form-field" required>
              <option value="">Sélectionner...</option>
              {availableBureaux.map((b) => <option key={b.idBureau} value={b.idBureau}>N° {b.numero} — {b.type} ({b.capacite} pl.)</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Date de début</label>
            <input type="date" value={formDate} onChange={(e) => setFormDate(e.target.value)} className="form-field" required />
          </div>
          <button type="submit" disabled={submitting} className="bg-primary px-5 py-2 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50" style={{ fontFamily: 'var(--font-mono)' }}>
            {submitting ? 'Affectation...' : 'Affecter'}
          </button>
        </form>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Active assignments */}
      <div className="th-label border-b border-border-subtle pb-2 mb-0">Affectations de poste actives</div>
      <div className="border border-border-subtle border-t-0 bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Agent</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Bureau</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Date de début</span></th>
              <th className="px-4 py-2.5 text-right"><span className="th-label">Action</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des affectations actives...</td></tr>
            ) : active.length === 0 ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucune affectation de poste active.</td></tr>
            ) : (
              active.map((a) => (
                <tr key={a.idAffectationPoste} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{a.agent.nom.toUpperCase()} {a.agent.prenom}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-primary font-semibold" style={{ fontFamily: 'var(--font-mono)' }}>{bureauLabel(a.bureauId)}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(a.dateAffectation).toLocaleDateString('fr-FR')}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-right">
                    <button onClick={() => handleClose(a)} className="btn-text-action btn-text-action-danger">Clôturer</button>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default AffectationsPoste;
