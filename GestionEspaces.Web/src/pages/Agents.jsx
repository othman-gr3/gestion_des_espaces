import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Agents = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentAgent, setCurrentAgent] = useState(null);
  const [formData, setFormData] = useState({ nom: '', prenom: '', matricule: '', email: '', telephone: '', fonction: '', departement: '', dateEmbauche: '', image: '' });
  const [formError, setFormError] = useState('');

  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [assignType, setAssignType] = useState('office');
  const [selectedAgentId, setSelectedAgentId] = useState(null);
  const [assignOptions, setAssignOptions] = useState([]);
  const [selectedOptionId, setSelectedOptionId] = useState('');
  const [assignError, setAssignError] = useState('');

  const [expandedAgentId, setExpandedAgentId] = useState(null);
  const [agentOfficeAssignments, setAgentOfficeAssignments] = useState([]);
  const [agentAssetAssignments, setAgentAssetAssignments] = useState([]);
  const [detailsLoading, setDetailsLoading] = useState(false);

  useEffect(() => { fetchAgents(); }, [page, searchText]);

  const fetchAgents = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/agents', { params: { searchText: searchText || undefined, pageNumber: page, pageSize } });
      setAgents(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load agents:', err);
      setError('Impossible de charger les agents.');
    } finally { setLoading(false); }
  };

  const fetchAgentAssignments = async (agentId) => {
    setDetailsLoading(true);
    try {
      const [officeRes, assetRes] = await Promise.all([api.get(`/agents/${agentId}/office-assignments`), api.get(`/agents/${agentId}/asset-assignments`)]);
      setAgentOfficeAssignments(officeRes.data || []);
      setAgentAssetAssignments(assetRes.data || []);
    } catch (err) { console.error('Failed to load assignments:', err); }
    finally { setDetailsLoading(false); }
  };

  const handleToggleExpand = (agentId) => {
    if (expandedAgentId === agentId) {
      setExpandedAgentId(null); setAgentOfficeAssignments([]); setAgentAssetAssignments([]);
    } else {
      setExpandedAgentId(agentId); fetchAgentAssignments(agentId);
    }
  };

  const handleOpenModal = (agent = null) => {
    setFormError('');
    if (agent) {
      setCurrentAgent(agent);
      setFormData({ nom: agent.nom, prenom: agent.prenom, matricule: agent.matricule, email: agent.email || '', telephone: agent.telephone || '', fonction: agent.fonction || '', departement: agent.departement || '', dateEmbauche: agent.dateEmbauche ? agent.dateEmbauche.split('T')[0] : '', image: agent.image || '' });
    } else {
      setCurrentAgent(null);
      setFormData({ nom: '', prenom: '', matricule: '', email: '', telephone: '', fonction: '', departement: '', dateEmbauche: '', image: '' });
    }
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      const payload = { ...formData, dateEmbauche: formData.dateEmbauche ? new Date(formData.dateEmbauche).toISOString() : null };
      if (currentAgent) {
        await api.put(`/agents/${currentAgent.idAgent}`, { concurrencyToken: currentAgent.concurrencyToken, ...payload });
      } else {
        await api.post('/agents', payload);
      }
      setIsModalOpen(false);
      fetchAgents();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  const handleDelete = async (agent) => {
    if (!window.confirm(`Supprimer l'agent "${agent.prenom} ${agent.nom}" ?`)) return;
    try {
      await api.delete(`/agents/${agent.idAgent}`, { data: { concurrencyToken: agent.concurrencyToken } });
      fetchAgents();
    } catch (err) {
      console.error('Delete error:', err);
      alert(err.response?.data?.detail || "Impossible de supprimer l'agent.");
    }
  };

  const handleOpenAssignModal = async (agentId, type) => {
    setSelectedAgentId(agentId); setAssignType(type); setSelectedOptionId(''); setAssignError(''); setAssignOptions([]); setIsAssignModalOpen(true);
    try {
      if (type === 'office') {
        const res = await api.get('/bureaux?statut=0&pageSize=100');
        setAssignOptions(res.data.items || []);
      } else {
        const res = await api.get('/actifs?pageSize=100');
        setAssignOptions(res.data.items || []);
      }
    } catch (err) { setAssignError('Impossible de charger les options disponibles.'); }
  };

  const handleAssignSubmit = async (e) => {
    e.preventDefault();
    setAssignError('');
    if (!selectedOptionId) { setAssignError('Veuillez sélectionner un choix.'); return; }
    try {
      if (assignType === 'office') {
        await api.post(`/agents/${selectedAgentId}/office-assignments`, { bureauId: parseInt(selectedOptionId), dateAffectation: new Date().toISOString() });
      } else {
        await api.post(`/agents/${selectedAgentId}/asset-assignments`, { actifId: parseInt(selectedOptionId), dateAffectation: new Date().toISOString() });
      }
      setIsAssignModalOpen(false);
      fetchAgentAssignments(selectedAgentId);
    } catch (err) {
      console.error('Assignment error:', err);
      setAssignError(err.response?.data?.detail || "Impossible d'effectuer l'affectation.");
    }
  };

  const handleCloseAssignment = async (assignmentId, type) => {
    if (!window.confirm("Clore cette affectation ?")) return;
    try {
      const endpoint = type === 'office' ? 'office-assignments' : 'asset-assignments';
      await api.delete(`/agents/${expandedAgentId}/${endpoint}/${assignmentId}`, { data: { dateFin: new Date().toISOString() } });
      fetchAgentAssignments(expandedAgentId);
    } catch (err) {
      console.error('Failed to close assignment:', err);
      alert(err.response?.data?.detail || "Impossible de clore l'affectation.");
    }
  };

  return (
    <div>
      {/* Toolbar */}
      <div className="flex items-center justify-between border-b-2 border-primary bg-surface-bg px-5 py-3 mb-6">
        <div className="flex items-center gap-3 flex-1 max-w-sm">
          <input
            type="text"
            placeholder="Rechercher nom, prénom, matricule..."
            value={searchText}
            onChange={(e) => { setSearchText(e.target.value); setPage(1); }}
            className="form-field flex-1"
          />
        </div>
        {isGestionnaire && (
          <button onClick={() => handleOpenModal()} className="ml-4 bg-primary px-5 py-2 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
            + Nouvel agent
          </button>
        )}
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="w-10 px-4 py-3" />
              <th className="px-6 py-3 text-left"><span className="th-label">Matricule</span></th>
              <th className="px-6 py-3 text-left"><span className="th-label">Nom / Prénom</span></th>
              <th className="px-6 py-3 text-left"><span className="th-label">Email / Tél.</span></th>
              <th className="px-6 py-3 text-left"><span className="th-label">Département</span></th>
              <th className="px-6 py-3 text-right"><span className="th-label">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Chargement des agents...</td></tr>
            ) : agents.length === 0 ? (
              <tr><td colSpan={6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Aucun agent trouvé.</td></tr>
            ) : (
              agents.map((agent) => {
                const isExpanded = expandedAgentId === agent.idAgent;
                return (
                  <React.Fragment key={agent.idAgent}>
                    <tr className="border-t border-border-subtle hover:bg-neutral-bg/50 transition-colors">
                      <td className="px-4 py-4 text-center">
                        <button onClick={() => handleToggleExpand(agent.idAgent)} className="text-[12px] text-text-secondary hover:text-primary transition-colors focus:outline-none w-5 h-5 flex items-center justify-center mx-auto">
                          {isExpanded ? '▼' : '▶'}
                        </button>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-[13px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{agent.matricule}</td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="text-[14px] font-semibold text-text-primary">{agent.nom.toUpperCase()} {agent.prenom}</div>
                        {agent.fonction && <div className="text-[12px] text-text-secondary mt-0.5">{agent.fonction}</div>}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="text-[13px] text-text-secondary">{agent.email || '—'}</div>
                        {agent.telephone && <div className="text-[12px] text-text-secondary opacity-75">{agent.telephone}</div>}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-primary">{agent.departement || '—'}</td>
                      <td className="whitespace-nowrap px-6 py-4 text-right">
                        {isGestionnaire && (
                          <div className="flex items-center justify-end gap-5">
                            <button onClick={() => handleOpenModal(agent)} className="btn-text-action btn-text-action-primary">Modifier</button>
                            <button onClick={() => handleDelete(agent)} className="btn-text-action btn-text-action-danger">Supprimer</button>
                          </div>
                        )}
                      </td>
                    </tr>

                    {isExpanded && (
                      <tr className="expand-row-enter">
                        <td colSpan={6} className="border-t border-b-2 border-primary bg-neutral-bg/40 px-0 py-0">
                          <div className="px-10 py-6 grid grid-cols-1 gap-8 md:grid-cols-2">
                            {/* Office */}
                            <div>
                              <div className="flex items-center justify-between border-b border-border-subtle pb-2 mb-4">
                                <span className="th-label">Affectation poste</span>
                                {isGestionnaire && (
                                  <button onClick={() => handleOpenAssignModal(agent.idAgent, 'office')} className="text-[12px] font-semibold text-accent hover:opacity-75 transition-opacity">+ Affecter</button>
                                )}
                              </div>
                              {detailsLoading ? (
                                <div className="text-[13px] text-text-secondary">Chargement...</div>
                              ) : agentOfficeAssignments.length === 0 ? (
                                <div className="text-[13px] text-text-secondary italic">Aucune affectation de poste active.</div>
                              ) : (
                                <div className="space-y-2">
                                  {agentOfficeAssignments.map((a) => (
                                    <div key={a.idAffectationPoste} className="flex items-center justify-between border-l-2 border-primary bg-surface-bg px-4 py-3">
                                      <div>
                                        <div className="text-[13px] font-semibold text-text-primary">Bureau N° {a.bureauNumero || a.bureauId}</div>
                                        <div className="text-[12px] text-text-secondary mt-0.5">
                                          {new Date(a.dateAffectation).toLocaleDateString('fr-FR')} → {a.dateFin ? new Date(a.dateFin).toLocaleDateString('fr-FR') : 'En cours'}
                                        </div>
                                      </div>
                                      {!a.dateFin && isGestionnaire && (
                                        <button onClick={() => handleCloseAssignment(a.idAffectationPoste, 'office')} className="btn-text-action btn-text-action-danger ml-4">Clore</button>
                                      )}
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>

                            {/* Assets */}
                            <div>
                              <div className="flex items-center justify-between border-b border-border-subtle pb-2 mb-4">
                                <span className="th-label">Actifs attribués</span>
                                {isGestionnaire && (
                                  <button onClick={() => handleOpenAssignModal(agent.idAgent, 'asset')} className="text-[12px] font-semibold text-accent hover:opacity-75 transition-opacity">+ Attribuer</button>
                                )}
                              </div>
                              {detailsLoading ? (
                                <div className="text-[13px] text-text-secondary">Chargement...</div>
                              ) : agentAssetAssignments.length === 0 ? (
                                <div className="text-[13px] text-text-secondary italic">Aucun matériel attribué sur cette fiche.</div>
                              ) : (
                                <div className="space-y-2">
                                  {agentAssetAssignments.map((a) => (
                                    <div key={a.idAffectationActif} className="flex items-center justify-between border-l-2 border-accent bg-surface-bg px-4 py-3">
                                      <div>
                                        <div className="text-[13px] font-semibold text-text-primary">{a.actifNom || `Actif ${a.actifId}`}</div>
                                        <div className="text-[12px] text-text-secondary mt-0.5">
                                          {new Date(a.dateAffectation).toLocaleDateString('fr-FR')} → {a.dateFin ? new Date(a.dateFin).toLocaleDateString('fr-FR') : 'En cours'}
                                        </div>
                                      </div>
                                      {!a.dateFin && isGestionnaire && (
                                        <button onClick={() => handleCloseAssignment(a.idAffectationActif, 'asset')} className="btn-text-action btn-text-action-danger ml-4">Clore</button>
                                      )}
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>
                          </div>
                        </td>
                      </tr>
                    )}
                  </React.Fragment>
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

      {/* Agent Form Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setIsModalOpen(false)}>
          <div className="modal-slide-in h-full w-full max-w-lg bg-surface-bg border-l-2 border-primary overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between border-b-2 border-primary px-6 py-4 bg-neutral-bg">
              <div>
                <div className="th-label mb-0.5">{currentAgent ? 'Modification' : 'Création'}</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>{currentAgent ? 'Fiche agent' : 'Nouvel agent'}</h3>
              </div>
              <button onClick={() => setIsModalOpen(false)} className="text-text-secondary hover:text-text-primary text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>
            {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
            <form onSubmit={handleSubmit} className="p-6 space-y-5">
              <div className="grid grid-cols-3 gap-5">
                <div className="col-span-2">
                  <label className="field-label">Nom</label>
                  <input type="text" value={formData.nom} onChange={(e) => setFormData((p) => ({ ...p, nom: e.target.value }))} className="form-field" required />
                </div>
                <div>
                  <label className="field-label">Prénom</label>
                  <input type="text" value={formData.prenom} onChange={(e) => setFormData((p) => ({ ...p, prenom: e.target.value }))} className="form-field" required />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Matricule</label>
                  <input type="text" value={formData.matricule} onChange={(e) => setFormData((p) => ({ ...p, matricule: e.target.value }))} className="form-field" required />
                </div>
                <div>
                  <label className="field-label">Département</label>
                  <input type="text" value={formData.departement} onChange={(e) => setFormData((p) => ({ ...p, departement: e.target.value }))} className="form-field" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Email</label>
                  <input type="email" value={formData.email} onChange={(e) => setFormData((p) => ({ ...p, email: e.target.value }))} className="form-field" />
                </div>
                <div>
                  <label className="field-label">Téléphone</label>
                  <input type="text" value={formData.telephone} onChange={(e) => setFormData((p) => ({ ...p, telephone: e.target.value }))} className="form-field" />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Fonction</label>
                  <input type="text" value={formData.fonction} onChange={(e) => setFormData((p) => ({ ...p, fonction: e.target.value }))} className="form-field" />
                </div>
                <div>
                  <label className="field-label">Date d'embauche</label>
                  <input type="date" value={formData.dateEmbauche} onChange={(e) => setFormData((p) => ({ ...p, dateEmbauche: e.target.value }))} className="form-field" />
                </div>
              </div>
              <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
                <button type="button" onClick={() => setIsModalOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
                <button type="submit" className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>Enregistrer</button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Assignment Modal */}
      {isAssignModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setIsAssignModalOpen(false)}>
          <div className="modal-slide-in h-full w-full max-w-sm bg-surface-bg border-l-2 border-accent overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between border-b-2 border-accent px-6 py-4 bg-neutral-bg">
              <div>
                <div className="th-label mb-0.5">Affectation</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
                  {assignType === 'office' ? 'Affecter un bureau' : 'Attribuer du matériel'}
                </h3>
              </div>
              <button onClick={() => setIsAssignModalOpen(false)} className="text-text-secondary hover:text-text-primary text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>
            {assignError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{assignError}</div>}
            <form onSubmit={handleAssignSubmit} className="p-6 space-y-5">
              <div>
                <label className="field-label">{assignType === 'office' ? 'Bureau disponible' : 'Matériel disponible'}</label>
                <select value={selectedOptionId} onChange={(e) => setSelectedOptionId(e.target.value)} className="form-field" required>
                  <option value="">Sélectionner...</option>
                  {assignType === 'office'
                    ? assignOptions.map((b) => <option key={b.idBureau} value={b.idBureau}>N° {b.numero} — {b.type} (Ét. {b.etage})</option>)
                    : assignOptions.map((a) => <option key={a.idActif} value={a.idActif}>{a.nom}{a.numeroSerie ? ` · S/N ${a.numeroSerie}` : ''}</option>)
                  }
                </select>
              </div>
              <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
                <button type="button" onClick={() => setIsAssignModalOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
                <button type="submit" className="bg-accent px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:opacity-90 transition-opacity" style={{ fontFamily: 'var(--font-mono)' }}>Confirmer</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Agents;
