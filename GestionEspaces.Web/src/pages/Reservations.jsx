import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const StatutConfig = {
  'EnAttente': { label: 'En attente', tag: 'ATTENTE', cls: 'bg-warning/10 text-warning border-l-2 border-warning' },
  'Confirmee': { label: 'Confirmée',  tag: 'CONF',    cls: 'bg-success/10 text-success border-l-2 border-success' },
  'Annulee':   { label: 'Annulée',    tag: 'ANN',     cls: 'bg-danger/10 text-danger border-l-2 border-danger' },
  'Rejetee':   { label: 'Rejetée',    tag: 'REJ',     cls: 'bg-danger/10 text-danger border-l-2 border-danger' },
};

const Reservations = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [reservations, setReservations] = useState([]);
  const [bureaux, setBureaux] = useState([]);
  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [selectedBureauId, setSelectedBureauId] = useState('');
  const [selectedAgentId, setSelectedAgentId] = useState('');
  const [statutFilter, setStatutFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formError, setFormError] = useState('');
  const [createData, setCreateData] = useState({ agentId: '', bureauId: '', dateDebut: '', dateFin: '', motif: '' });

  useEffect(() => { fetchInitialData(); }, []);
  useEffect(() => { fetchReservations(); }, [page, selectedBureauId, selectedAgentId, statutFilter, dateFrom, dateTo]);

  const fetchInitialData = async () => {
    try {
      const [bureauxRes, agentsRes] = await Promise.all([api.get('/bureaux?pageSize=100'), api.get('/agents?pageSize=100')]);
      setBureaux(bureauxRes.data.items || []);
      setAgents(agentsRes.data.items || []);
    } catch (err) { console.error('Failed to load booking data:', err); }
  };

  const fetchReservations = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/reservations', {
        params: { bureauId: selectedBureauId || undefined, agentId: selectedAgentId || undefined, from: dateFrom ? new Date(dateFrom).toISOString() : undefined, to: dateTo ? new Date(dateTo).toISOString() : undefined, statut: statutFilter || undefined, pageNumber: page, pageSize },
      });
      setReservations(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bookings:', err);
      setError('Impossible de charger les réservations.');
    } finally { setLoading(false); }
  };

  const handleCreateSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    const start = new Date(createData.dateDebut);
    const end = new Date(createData.dateFin);
    if (end <= start) { setFormError('La date de fin doit être postérieure à la date de début.'); return; }
    try {
      await api.post(`/reservations/agents/${createData.agentId}`, { bureauId: parseInt(createData.bureauId), dateDebut: start.toISOString(), dateFin: end.toISOString(), motif: createData.motif || null });
      setIsModalOpen(false);
      fetchReservations();
    } catch (err) {
      console.error('Booking creation error:', err);
      setFormError(err.response?.data?.detail || 'Impossible de créer la réservation.');
    }
  };

  const handleWorkflowAction = async (id, action, concurrencyToken) => {
    try {
      await api.post(`/reservations/${id}/${action}`, { concurrencyToken });
      fetchReservations();
    } catch (err) {
      console.error(`Workflow ${action} error:`, err);
      alert(err.response?.data?.detail || "Une erreur est survenue lors de l'action.");
    }
  };

  return (
    <div>
      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-5 py-3 mb-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-5 flex-1">
            <div>
              <label className="field-label">Agent</label>
              <select value={selectedAgentId} onChange={(e) => { setSelectedAgentId(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous les agents</option>
                {agents.map((a) => <option key={a.idAgent} value={a.idAgent}>{a.nom} {a.prenom}</option>)}
              </select>
            </div>
            <div>
              <label className="field-label">Espace</label>
              <select value={selectedBureauId} onChange={(e) => { setSelectedBureauId(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous</option>
                {bureaux.map((b) => <option key={b.idBureau} value={b.idBureau}>N° {b.numero}</option>)}
              </select>
            </div>
            <div>
              <label className="field-label">Statut</label>
              <select value={statutFilter} onChange={(e) => { setStatutFilter(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous</option>
                <option value="EnAttente">En attente</option>
                <option value="Confirmee">Confirmée</option>
                <option value="Annulee">Annulée</option>
                <option value="Rejetee">Rejetée</option>
              </select>
            </div>
            <div>
              <label className="field-label">Du</label>
              <input type="date" value={dateFrom} onChange={(e) => { setDateFrom(e.target.value); setPage(1); }} className="form-field" />
            </div>
            <div>
              <label className="field-label">Au</label>
              <input type="date" value={dateTo} onChange={(e) => { setDateTo(e.target.value); setPage(1); }} className="form-field" />
            </div>
          </div>
          <button
            onClick={() => {
              setFormError('');
              setCreateData({ agentId: agents[0]?.idAgent || '', bureauId: bureaux[0]?.idBureau || '', dateDebut: '', dateFin: '', motif: '' });
              setIsModalOpen(true);
            }}
            className="sm:ml-4 self-end bg-accent px-5 py-2 text-[12px] font-semibold uppercase tracking-wider text-white hover:opacity-90 transition-opacity flex-shrink-0"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            + Réserver un créneau
          </button>
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              {['Espace', 'Bénéficiaire', 'Créneau', 'Motif', 'Statut', 'Actions'].map((h, i) => (
                <th key={i} className={`px-6 py-3 ${i === 5 ? 'text-right' : 'text-left'}`}>
                  <span className="th-label">{h}</span>
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Chargement des réservations...</td></tr>
            ) : reservations.length === 0 ? (
              <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Aucune réservation ne correspond aux filtres.</td></tr>
            ) : (
              reservations.map((r) => {
                const b = bureaux.find((x) => x.idBureau === r.idBureau);
                const a = agents.find((x) => x.idAgent === r.idAgent);
                const isPending = r.statut === 'EnAttente';
                const st = StatutConfig[r.statut] || { label: r.statut, tag: r.statut, cls: '' };
                return (
                  <tr key={r.idReservation} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>
                      {b ? `Bureau N° ${b.numero}` : `Espace ${r.idBureau}`}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-[14px] font-medium text-text-primary">
                      {a ? `${a.nom.toUpperCase()} ${a.prenom}` : `Agent ${r.idAgent}`}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <div className="text-[13px] text-text-secondary">{new Date(r.dateDebut).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</div>
                      <div className="text-[12px] text-text-secondary opacity-70">→ {new Date(r.dateFin).toLocaleString('fr-FR', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' })}</div>
                    </td>
                    <td className="px-6 py-4 text-[13px] text-text-secondary max-w-[160px] truncate">{r.motif || '—'}</td>
                    <td className="whitespace-nowrap px-6 py-4">
                      <span className={`status-tag ${st.cls}`}>{st.tag}</span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-4">
                        {isPending && isGestionnaire && (
                          <>
                            <button onClick={() => handleWorkflowAction(r.idReservation, 'confirmer', r.concurrencyToken)} className="btn-text-action btn-text-action-success">Valider</button>
                            <button onClick={() => handleWorkflowAction(r.idReservation, 'rejeter', r.concurrencyToken)} className="btn-text-action btn-text-action-danger">Rejeter</button>
                          </>
                        )}
                        {r.statut !== 'Annulee' && r.statut !== 'Rejetee' && (
                          <button onClick={() => handleWorkflowAction(r.idReservation, 'annuler', r.concurrencyToken)} className="btn-text-action btn-text-action-muted">Annuler</button>
                        )}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalCount > pageSize && (
        <div className="flex items-center justify-between border-t border-border-subtle pt-4 mt-4">
          <button disabled={page === 1} onClick={() => setPage((p) => Math.max(p - 1, 1))} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40">← Précédent</button>
          <span className="text-[12px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>Page {page} / {Math.ceil(totalCount / pageSize)}</span>
          <button disabled={page * pageSize >= totalCount} onClick={() => setPage((p) => p + 1)} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40">Suivant →</button>
        </div>
      )}

      {/* Booking modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setIsModalOpen(false)}>
          <div className="modal-slide-in h-full w-full max-w-lg bg-surface-bg border-l-2 border-accent overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between border-b-2 border-accent px-6 py-4 bg-neutral-bg">
              <div>
                <div className="th-label mb-0.5">Nouvelle réservation</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>Réserver un créneau</h3>
              </div>
              <button onClick={() => setIsModalOpen(false)} className="text-text-secondary hover:text-text-primary text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>
            {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
            <form onSubmit={handleCreateSubmit} className="p-6 space-y-5">
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Bénéficiaire</label>
                  <select value={createData.agentId} onChange={(e) => setCreateData((p) => ({ ...p, agentId: e.target.value }))} className="form-field" required>
                    <option value="">Sélectionner...</option>
                    {agents.map((a) => <option key={a.idAgent} value={a.idAgent}>{a.nom.toUpperCase()} {a.prenom}</option>)}
                  </select>
                </div>
                <div>
                  <label className="field-label">Espace / Bureau</label>
                  <select value={createData.bureauId} onChange={(e) => setCreateData((p) => ({ ...p, bureauId: e.target.value }))} className="form-field" required>
                    <option value="">Sélectionner...</option>
                    {bureaux.map((b) => <option key={b.idBureau} value={b.idBureau}>N° {b.numero} — {b.type}</option>)}
                  </select>
                </div>
              </div>
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Début</label>
                  <input type="datetime-local" value={createData.dateDebut} onChange={(e) => setCreateData((p) => ({ ...p, dateDebut: e.target.value }))} className="form-field" required />
                </div>
                <div>
                  <label className="field-label">Fin</label>
                  <input type="datetime-local" value={createData.dateFin} onChange={(e) => setCreateData((p) => ({ ...p, dateFin: e.target.value }))} className="form-field" required />
                </div>
              </div>
              <div>
                <label className="field-label">Motif</label>
                <textarea
                  value={createData.motif}
                  onChange={(e) => setCreateData((p) => ({ ...p, motif: e.target.value }))}
                  className="form-field resize-none"
                  rows="3"
                  placeholder="Ex: Comité de direction — réunion mensuelle"
                />
              </div>
              <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
                <button type="button" onClick={() => setIsModalOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
                <button type="submit" className="bg-accent px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:opacity-90 transition-opacity" style={{ fontFamily: 'var(--font-mono)' }}>Enregistrer</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Reservations;
