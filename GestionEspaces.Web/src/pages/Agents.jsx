import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Agents = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Search/Filters
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Forms & Modal states
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentAgent, setCurrentAgent] = useState(null);
  const [formData, setFormData] = useState({
    nom: '',
    prenom: '',
    matricule: '',
    email: '',
    telephone: '',
    fonction: '',
    departement: '',
    dateEmbauche: '',
    image: '',
  });
  const [formError, setFormError] = useState('');

  // Assignment Modal states
  const [isAssignModalOpen, setIsAssignModalOpen] = useState(false);
  const [assignType, setAssignType] = useState('office'); // 'office' or 'asset'
  const [selectedAgentId, setSelectedAgentId] = useState(null);
  const [assignOptions, setAssignOptions] = useState([]); // list of available bureaux or actifs
  const [selectedOptionId, setSelectedOptionId] = useState('');
  const [assignError, setAssignError] = useState('');

  // Expandable row for details & assignments
  const [expandedAgentId, setExpandedAgentId] = useState(null);
  const [agentOfficeAssignments, setAgentOfficeAssignments] = useState([]);
  const [agentAssetAssignments, setAgentAssetAssignments] = useState([]);
  const [detailsLoading, setDetailsLoading] = useState(false);

  useEffect(() => {
    fetchAgents();
  }, [page, searchText]);

  const fetchAgents = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/agents', {
        params: {
          searchText: searchText || undefined,
          pageNumber: page,
          pageSize: pageSize,
        },
      });
      setAgents(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load agents:', err);
      setError('Impossible de charger les agents.');
    } finally {
      setLoading(false);
    }
  };

  const fetchAgentAssignments = async (agentId) => {
    setDetailsLoading(true);
    try {
      const [officeRes, assetRes] = await Promise.all([
        api.get(`/agents/${agentId}/office-assignments`),
        api.get(`/agents/${agentId}/asset-assignments`),
      ]);
      setAgentOfficeAssignments(officeRes.data || []);
      setAgentAssetAssignments(assetRes.data || []);
    } catch (err) {
      console.error('Failed to load assignments:', err);
    } finally {
      setDetailsLoading(false);
    }
  };

  const handleToggleExpand = (agentId) => {
    if (expandedAgentId === agentId) {
      setExpandedAgentId(null);
      setAgentOfficeAssignments([]);
      setAgentAssetAssignments([]);
    } else {
      setExpandedAgentId(agentId);
      fetchAgentAssignments(agentId);
    }
  };

  const handleOpenModal = (agent = null) => {
    setFormError('');
    if (agent) {
      setCurrentAgent(agent);
      setFormData({
        nom: agent.nom,
        prenom: agent.prenom,
        matricule: agent.matricule,
        email: agent.email || '',
        telephone: agent.telephone || '',
        fonction: agent.fonction || '',
        departement: agent.departement || '',
        dateEmbauche: agent.dateEmbauche ? agent.dateEmbauche.split('T')[0] : '',
        image: agent.image || '',
      });
    } else {
      setCurrentAgent(null);
      setFormData({
        nom: '',
        prenom: '',
        matricule: '',
        email: '',
        telephone: '',
        fonction: '',
        departement: '',
        dateEmbauche: '',
        image: '',
      });
    }
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      const payload = {
        ...formData,
        dateEmbauche: formData.dateEmbauche ? new Date(formData.dateEmbauche).toISOString() : null,
      };

      if (currentAgent) {
        await api.put(`/agents/${currentAgent.idAgent}`, {
          concurrencyToken: currentAgent.concurrencyToken,
          ...payload,
        });
      } else {
        await api.post('/agents', payload);
      }
      setIsModalOpen(false);
      fetchAgents();
    } catch (err) {
      console.error('Submit error:', err);
      const detail = err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.";
      setFormError(detail);
    }
  };

  const handleDelete = async (agent) => {
    if (!window.confirm(`Êtes-vous sûr de vouloir supprimer l'agent "${agent.prenom} ${agent.nom}" ?`)) return;
    try {
      await api.delete(`/agents/${agent.idAgent}`, {
        data: { concurrencyToken: agent.concurrencyToken }
      });
      fetchAgents();
    } catch (err) {
      console.error('Delete error:', err);
      const detail = err.response?.data?.detail || "Impossible de supprimer l'agent.";
      alert(detail);
    }
  };

  // Assignment logic
  const handleOpenAssignModal = async (agentId, type) => {
    setSelectedAgentId(agentId);
    setAssignType(type);
    setSelectedOptionId('');
    setAssignError('');
    setAssignOptions([]);
    setIsAssignModalOpen(true);

    try {
      if (type === 'office') {
        const res = await api.get('/bureaux?statut=0&pageSize=100');
        setAssignOptions(res.data.items || []);
      } else {
        const res = await api.get('/actifs?pageSize=100');
        setAssignOptions(res.data.items || []);
      }
    } catch (err) {
      console.error('Failed to load assignment options:', err);
      setAssignError('Impossible de charger les options disponibles.');
    }
  };

  const handleAssignSubmit = async (e) => {
    e.preventDefault();
    setAssignError('');
    if (!selectedOptionId) {
      setAssignError('Veuillez sélectionner un choix.');
      return;
    }

    try {
      if (assignType === 'office') {
        await api.post(`/agents/${selectedAgentId}/office-assignments`, {
          bureauId: parseInt(selectedOptionId),
          dateAffectation: new Date().toISOString(),
        });
      } else {
        await api.post(`/agents/${selectedAgentId}/asset-assignments`, {
          actifId: parseInt(selectedOptionId),
          dateAffectation: new Date().toISOString(),
        });
      }
      setIsAssignModalOpen(false);
      fetchAgentAssignments(selectedAgentId);
    } catch (err) {
      console.error('Assignment error:', err);
      const detail = err.response?.data?.detail || "Impossible d'effectuer l'affectation.";
      setAssignError(detail);
    }
  };

  const handleCloseAssignment = async (assignmentId, type) => {
    if (!window.confirm("Êtes-vous sûr de vouloir clore cette affectation ?")) return;
    try {
      const endpoint = type === 'office' ? 'office-assignments' : 'asset-assignments';
      // Close affectation with finish date payload
      await api.delete(`/agents/${expandedAgentId}/${endpoint}/${assignmentId}`, {
        data: { dateFin: new Date().toISOString() }
      });
      fetchAgentAssignments(expandedAgentId);
    } catch (err) {
      console.error('Failed to close assignment:', err);
      alert(err.response?.data?.detail || "Impossible de clore l'affectation.");
    }
  };

  return (
    <div className="space-y-6">
      {/* Search & Actions Bar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border border-border-subtle bg-surface-bg p-4 rounded-lg shadow-sm">
        <div className="flex max-w-sm flex-1 gap-2">
          <input
            type="text"
            placeholder="Rechercher nom, prénom, matricule..."
            value={searchText}
            onChange={(e) => {
              setSearchText(e.target.value);
              setPage(1);
            }}
            className="w-full rounded border border-border-subtle bg-surface-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:outline-none"
          />
        </div>

        {isGestionnaire && (
          <button
            onClick={() => handleOpenModal()}
            className="rounded bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none"
          >
            Nouvel Agent
          </button>
        )}
      </div>

      {error && (
        <div className="rounded bg-danger/10 border border-danger/20 p-4 text-sm font-semibold text-danger">
          {error}
        </div>
      )}

      {/* Agents Table */}
      <div className="overflow-hidden border border-border-subtle bg-surface-bg rounded-lg shadow-sm">
        <table className="min-w-full divide-y divide-border-subtle">
          <thead className="bg-neutral-bg">
            <tr>
              <th className="w-10 px-6 py-3"></th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Matricule</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Nom / Prénom</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Email / Tél</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Département</th>
              <th className="px-6 py-3 text-right text-xs font-bold uppercase tracking-wider text-text-secondary">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Chargement des agents...
                </td>
              </tr>
            ) : agents.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Aucun agent trouvé.
                </td>
              </tr>
            ) : (
              agents.map((agent) => {
                const isExpanded = expandedAgentId === agent.idAgent;
                return (
                  <React.Fragment key={agent.idAgent}>
                    <tr className="hover:bg-neutral-bg/30">
                      <td className="px-6 py-4 text-center">
                        <button
                          onClick={() => handleToggleExpand(agent.idAgent)}
                          className="text-text-secondary font-bold hover:text-primary transition-colors focus:outline-none"
                        >
                          {isExpanded ? '▼' : '▶'}
                        </button>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm font-bold text-primary">{agent.matricule}</td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-text-primary">
                        {agent.nom.toUpperCase()} {agent.prenom}
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm text-text-secondary">
                        <div>{agent.email || '-'}</div>
                        <div className="text-xs">{agent.telephone || ''}</div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">
                        <div>{agent.departement || '-'}</div>
                        <div className="text-xs text-text-secondary">{agent.fonction || ''}</div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-medium space-x-3">
                        {isGestionnaire && (
                          <>
                            <button
                              onClick={() => handleOpenModal(agent)}
                              className="text-primary hover:text-primary-dark transition-colors"
                            >
                              Modifier
                            </button>
                            <button
                              onClick={() => handleDelete(agent)}
                              className="text-danger hover:text-red-700 transition-colors"
                            >
                              Supprimer
                            </button>
                          </>
                        )}
                      </td>
                    </tr>

                    {/* Expandable row: Assignments and detailed view */}
                    {isExpanded && (
                      <tr className="bg-neutral-bg/25">
                        <td colSpan={6} className="px-12 py-6 border-y border-border-subtle">
                          <div className="grid grid-cols-1 gap-8 md:grid-cols-2">
                            {/* Desk Assignments */}
                            <div className="space-y-4">
                              <div className="flex items-center justify-between border-b border-border-subtle pb-2">
                                <h4 className="text-xs font-bold uppercase tracking-wider text-text-primary">Affectation Poste</h4>
                                {isGestionnaire && (
                                  <button
                                    onClick={() => handleOpenAssignModal(agent.idAgent, 'office')}
                                    className="text-[10px] font-bold text-accent bg-accent/10 px-2 py-1 rounded hover:bg-accent hover:text-white transition-colors"
                                  >
                                    + Affecter
                                  </button>
                                )}
                              </div>

                              {detailsLoading ? (
                                <div className="text-xs text-text-secondary">Chargement...</div>
                              ) : agentOfficeAssignments.length === 0 ? (
                                <div className="text-xs text-text-secondary italic">Aucune affectation de poste.</div>
                              ) : (
                                <div className="space-y-2">
                                  {agentOfficeAssignments.map((a) => (
                                    <div key={a.idAffectationPoste} className="flex items-center justify-between p-3 border border-border-subtle bg-surface-bg rounded">
                                      <div className="text-xs text-text-primary">
                                        <div className="font-bold">Bureau N° {a.bureauNumero || a.bureauId}</div>
                                        <div className="text-text-secondary mt-0.5">
                                          Du {new Date(a.dateAffectation).toLocaleDateString()}
                                          {a.dateFin ? ` au ${new Date(a.dateFin).toLocaleDateString()}` : ' (Actif)'}
                                        </div>
                                      </div>
                                      {!a.dateFin && isGestionnaire && (
                                        <button
                                          onClick={() => handleCloseAssignment(a.idAffectationPoste, 'office')}
                                          className="text-[10px] font-bold text-danger hover:underline"
                                        >
                                          Clore
                                        </button>
                                      )}
                                    </div>
                                  ))}
                                </div>
                              )}
                            </div>

                            {/* Asset Assignments */}
                            <div className="space-y-4">
                              <div className="flex items-center justify-between border-b border-border-subtle pb-2">
                                <h4 className="text-xs font-bold uppercase tracking-wider text-text-primary">Affectations Actifs (Matériel)</h4>
                                {isGestionnaire && (
                                  <button
                                    onClick={() => handleOpenAssignModal(agent.idAgent, 'asset')}
                                    className="text-[10px] font-bold text-accent bg-accent/10 px-2 py-1 rounded hover:bg-accent hover:text-white transition-colors"
                                  >
                                    + Attribuer matériel
                                  </button>
                                )}
                              </div>

                              {detailsLoading ? (
                                <div className="text-xs text-text-secondary">Chargement...</div>
                              ) : agentAssetAssignments.length === 0 ? (
                                <div className="text-xs text-text-secondary italic">Aucun matériel attribué.</div>
                              ) : (
                                <div className="space-y-2">
                                  {agentAssetAssignments.map((a) => (
                                    <div key={a.idAffectationActif} className="flex items-center justify-between p-3 border border-border-subtle bg-surface-bg rounded">
                                      <div className="text-xs text-text-primary">
                                        <div className="font-bold">{a.actifNom || `Matériel ${a.actifId}`}</div>
                                        <div className="text-text-secondary mt-0.5">
                                          Du {new Date(a.dateAffectation).toLocaleDateString()}
                                          {a.dateFin ? ` au ${new Date(a.dateFin).toLocaleDateString()}` : ' (Actif)'}
                                        </div>
                                      </div>
                                      {!a.dateFin && isGestionnaire && (
                                        <button
                                          onClick={() => handleCloseAssignment(a.idAffectationActif, 'asset')}
                                          className="text-[10px] font-bold text-danger hover:underline"
                                        >
                                          Clore
                                        </button>
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
        <div className="flex items-center justify-between border-t border-border-subtle pt-4">
          <button
            disabled={page === 1}
            onClick={() => setPage((p) => Math.max(p - 1, 1))}
            className="rounded border border-border-subtle bg-surface-bg px-3 py-1.5 text-xs font-semibold text-text-primary hover:bg-neutral-bg disabled:opacity-50"
          >
            Précédent
          </button>
          <span className="text-xs text-text-secondary">
            Page {page} sur {Math.ceil(totalCount / pageSize)}
          </span>
          <button
            disabled={page * pageSize >= totalCount}
            onClick={() => setPage((p) => p + 1)}
            className="rounded border border-border-subtle bg-surface-bg px-3 py-1.5 text-xs font-semibold text-text-primary hover:bg-neutral-bg disabled:opacity-50"
          >
            Suivant
          </button>
        </div>
      )}

      {/* Form Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-lg border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-lg">
            <h3 className="text-lg font-bold text-text-primary mb-4">
              {currentAgent ? "Modifier l'agent" : 'Ajouter un agent'}
            </h3>

            {formError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-3 gap-4">
                <div className="col-span-2">
                  <label className="block text-xs font-bold text-text-secondary uppercase">Nom</label>
                  <input
                    type="text"
                    value={formData.nom}
                    onChange={(e) => setFormData((p) => ({ ...p, nom: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Prénom</label>
                  <input
                    type="text"
                    value={formData.prenom}
                    onChange={(e) => setFormData((p) => ({ ...p, prenom: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Matricule</label>
                  <input
                    type="text"
                    value={formData.matricule}
                    onChange={(e) => setFormData((p) => ({ ...p, matricule: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Département</label>
                  <input
                    type="text"
                    value={formData.departement}
                    onChange={(e) => setFormData((p) => ({ ...p, departement: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Email</label>
                  <input
                    type="email"
                    value={formData.email}
                    onChange={(e) => setFormData((p) => ({ ...p, email: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Téléphone</label>
                  <input
                    type="text"
                    value={formData.telephone}
                    onChange={(e) => setFormData((p) => ({ ...p, telephone: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Fonction</label>
                  <input
                    type="text"
                    value={formData.fonction}
                    onChange={(e) => setFormData((p) => ({ ...p, fonction: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Date d'embauche</label>
                  <input
                    type="date"
                    value={formData.dateEmbauche}
                    onChange={(e) => setFormData((p) => ({ ...p, dateEmbauche: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-text-secondary uppercase">Image URL (optionnel)</label>
                <input
                  type="text"
                  value={formData.image}
                  onChange={(e) => setFormData((p) => ({ ...p, image: e.target.value }))}
                  className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                />
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-border-subtle">
                <button
                  type="button"
                  onClick={() => setIsModalOpen(false)}
                  className="rounded border border-border-subtle bg-surface-bg px-4 py-2 text-sm font-semibold text-text-primary hover:bg-neutral-bg focus:outline-none"
                >
                  Annuler
                </button>
                <button
                  type="submit"
                  className="rounded bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none"
                >
                  Enregistrer
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Assignment Modal (Office or Asset) */}
      {isAssignModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-md border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-lg">
            <h3 className="text-sm font-bold uppercase tracking-wider text-text-primary mb-4">
              {assignType === 'office' ? "Affecter un bureau" : "Attribuer un matériel"}
            </h3>

            {assignError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {assignError}
              </div>
            )}

            <form onSubmit={handleAssignSubmit} className="space-y-4">
              <div>
                <label className="block text-xs font-bold text-text-secondary uppercase">
                  {assignType === 'office' ? "Bureau Disponible" : "Matériel Disponible"}
                </label>
                <select
                  value={selectedOptionId}
                  onChange={(e) => setSelectedOptionId(e.target.value)}
                  className="mt-2 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:bg-white focus:outline-none"
                  required
                >
                  <option value="">Sélectionner...</option>
                  {assignType === 'office'
                    ? assignOptions.map((b) => (
                        <option key={b.idBureau} value={b.idBureau}>
                          N° {b.numero} - {b.type} (Étage {b.etage})
                        </option>
                      ))
                    : assignOptions.map((a) => (
                        <option key={a.idActif} value={a.idActif}>
                          {a.nom} {a.numeroSerie ? `(S/N: ${a.numeroSerie})` : ''}
                        </option>
                      ))}
                </select>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-border-subtle">
                <button
                  type="button"
                  onClick={() => setIsAssignModalOpen(false)}
                  className="rounded border border-border-subtle bg-surface-bg px-4 py-2 text-sm font-semibold text-text-primary hover:bg-neutral-bg focus:outline-none"
                >
                  Annuler
                </button>
                <button
                  type="submit"
                  className="rounded bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none"
                >
                  Confirmer l'affectation
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Agents;
