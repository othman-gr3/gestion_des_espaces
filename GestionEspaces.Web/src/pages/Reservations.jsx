import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const StatutLabels = {
  'EnAttente': { label: 'En Attente', class: 'bg-warning/10 text-warning border border-warning/20' },
  'Confirmee': { label: 'Confirmée', class: 'bg-success/10 text-success border border-success/20' },
  'Annulee': { label: 'Annulée', class: 'bg-danger/10 text-danger border border-danger/20' },
  'Rejetee': { label: 'Rejetée', class: 'bg-danger/10 text-danger border border-danger/20' },
};

const Reservations = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [reservations, setReservations] = useState([]);
  const [bureaux, setBureaux] = useState([]);
  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Search Filters
  const [selectedBureauId, setSelectedBureauId] = useState('');
  const [selectedAgentId, setSelectedAgentId] = useState('');
  const [statutFilter, setStatutFilter] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');

  // Pagination state
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Form Modal state (Create)
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [formError, setFormError] = useState('');
  const [createData, setCreateData] = useState({
    agentId: '',
    bureauId: '',
    dateDebut: '',
    dateFin: '',
    motif: '',
  });

  useEffect(() => {
    fetchInitialData();
  }, []);

  useEffect(() => {
    fetchReservations();
  }, [page, selectedBureauId, selectedAgentId, statutFilter, dateFrom, dateTo]);

  const fetchInitialData = async () => {
    try {
      const [bureauxRes, agentsRes] = await Promise.all([
        api.get('/bureaux?pageSize=100'),
        api.get('/agents?pageSize=100'),
      ]);
      setBureaux(bureauxRes.data.items || []);
      setAgents(agentsRes.data.items || []);
    } catch (err) {
      console.error('Failed to load initial booking data:', err);
    }
  };

  const fetchReservations = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/reservations', {
        params: {
          bureauId: selectedBureauId || undefined,
          agentId: selectedAgentId || undefined,
          from: dateFrom ? new Date(dateFrom).toISOString() : undefined,
          to: dateTo ? new Date(dateTo).toISOString() : undefined,
          statut: statutFilter || undefined,
          pageNumber: page,
          pageSize: pageSize,
        },
      });
      setReservations(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bookings:', err);
      setError('Impossible de charger les réservations.');
    } finally {
      setLoading(false);
    }
  };

  const handleCreateSubmit = async (e) => {
    e.preventDefault();
    setFormError('');

    const start = new Date(createData.dateDebut);
    const end = new Date(createData.dateFin);

    if (end <= start) {
      setFormError('La date de fin doit être postérieure à la date de début.');
      return;
    }

    try {
      await api.post(`/reservations/agents/${createData.agentId}`, {
        bureauId: parseInt(createData.bureauId),
        dateDebut: start.toISOString(),
        dateFin: end.toISOString(),
        motif: createData.motif || null,
      });
      setIsModalOpen(false);
      fetchReservations();
    } catch (err) {
      console.error('Booking creation error:', err);
      const detail = err.response?.data?.detail || 'Impossible de créer la réservation.';
      setFormError(detail);
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
    <div className="space-y-6">
      {/* Filters Toolbar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border border-border-subtle bg-surface-bg p-4 rounded-lg shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-5 flex-1">
          {/* Agent Filter */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Agent</label>
            <select
              value={selectedAgentId}
              onChange={(e) => {
                setSelectedAgentId(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les agents</option>
              {agents.map((a) => (
                <option key={a.idAgent} value={a.idAgent}>
                  {a.nom} {a.prenom}
                </option>
              ))}
            </select>
          </div>

          {/* Bureau Filter */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Salle / Bureau</label>
            <select
              value={selectedBureauId}
              onChange={(e) => {
                setSelectedBureauId(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les espaces</option>
              {bureaux.map((b) => (
                <option key={b.idBureau} value={b.idBureau}>
                  N° {b.numero} - {b.type}
                </option>
              ))}
            </select>
          </div>

          {/* Status Filter */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Statut</label>
            <select
              value={statutFilter}
              onChange={(e) => {
                setStatutFilter(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les statuts</option>
              <option value="EnAttente">En Attente</option>
              <option value="Confirmee">Confirmée</option>
              <option value="Annulee">Annulée</option>
              <option value="Rejetee">Rejetée</option>
            </select>
          </div>

          {/* Date Range From */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Du</label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => {
                setDateFrom(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1 text-xs text-text-primary focus:bg-white focus:outline-none"
            />
          </div>

          {/* Date Range To */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Au</label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => {
                setDateTo(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1 text-xs text-text-primary focus:bg-white focus:outline-none"
            />
          </div>
        </div>

        {/* Highlight action - Nouvelle réservation */}
        <button
          onClick={() => {
            setFormError('');
            setCreateData({
              agentId: agents[0]?.idAgent || '',
              bureauId: bureaux[0]?.idBureau || '',
              dateDebut: '',
              dateFin: '',
              motif: '',
            });
            setIsModalOpen(true);
          }}
          className="self-end rounded bg-accent px-4 py-2 text-sm font-bold text-white hover:bg-[#b88c1c] transition-colors focus:outline-none"
        >
          Nouvelle réservation
        </button>
      </div>

      {error && (
        <div className="rounded bg-danger/10 border border-danger/20 p-4 text-sm font-semibold text-danger">
          {error}
        </div>
      )}

      {/* Bookings Table */}
      <div className="overflow-hidden border border-border-subtle bg-surface-bg rounded-lg shadow-sm">
        <table className="min-w-full divide-y divide-border-subtle">
          <thead className="bg-neutral-bg">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Espace / Bureau</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Bénéficiaire</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Créneau Horaire (UTC)</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Motif</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Statut</th>
              <th className="px-6 py-3 text-right text-xs font-bold uppercase tracking-wider text-text-secondary">Actions</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Chargement des réservations...
                </td>
              </tr>
            ) : reservations.length === 0 ? (
              <tr>
                <td colSpan={6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Aucune réservation trouvée.
                </td>
              </tr>
            ) : (
              reservations.map((r, index) => {
                const b = bureaux.find((x) => x.idBureau === r.idBureau);
                const a = agents.find((x) => x.idAgent === r.idAgent);
                const isPending = r.statut === 'EnAttente';

                return (
                  <tr key={r.idReservation} className={index % 2 === 0 ? 'bg-surface-bg' : 'bg-neutral-bg/50'}>
                    <td className="whitespace-nowrap px-6 py-4 text-sm font-bold text-primary">
                      {b ? `Bureau N° ${b.numero}` : `Salle ${r.idBureau}`}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">
                      {a ? `${a.nom.toUpperCase()} ${a.prenom}` : `Agent ${r.idAgent}`}
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm text-text-secondary">
                      <div>Du {new Date(r.dateDebut).toLocaleString('fr-FR')}</div>
                      <div className="text-xs">Au {new Date(r.dateFin).toLocaleString('fr-FR')}</div>
                    </td>
                    <td className="px-6 py-4 text-sm text-text-secondary max-w-xs truncate">{r.motif || '-'}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-sm">
                      <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${StatutLabels[r.statut]?.class || ''}`}>
                        {StatutLabels[r.statut]?.label || r.statut}
                      </span>
                    </td>
                    <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-bold space-x-3">
                      {isPending && isGestionnaire && (
                        <>
                          <button
                            onClick={() => handleWorkflowAction(r.idReservation, 'confirmer', r.concurrencyToken)}
                            className="text-success hover:underline"
                          >
                            Valider
                          </button>
                          <button
                            onClick={() => handleWorkflowAction(r.idReservation, 'rejeter', r.concurrencyToken)}
                            className="text-danger hover:underline"
                          >
                            Rejeter
                          </button>
                        </>
                      )}
                      {r.statut !== 'Annulee' && r.statut !== 'Rejetee' && (
                        <button
                          onClick={() => handleWorkflowAction(r.idReservation, 'annuler', r.concurrencyToken)}
                          className="text-text-secondary hover:text-text-primary transition-colors"
                        >
                          Annuler
                        </button>
                      )}
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

      {/* Booking Form Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-lg border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-lg">
            <h3 className="text-lg font-bold text-text-primary mb-4">Nouvelle Réservation de Salle</h3>

            {formError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {formError}
              </div>
            )}

            <form onSubmit={handleCreateSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Bénéficiaire</label>
                  <select
                    value={createData.agentId}
                    onChange={(e) => setCreateData((p) => ({ ...p, agentId: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:bg-white focus:outline-none"
                    required
                  >
                    <option value="">Sélectionner...</option>
                    {agents.map((a) => (
                      <option key={a.idAgent} value={a.idAgent}>
                        {a.nom.toUpperCase()} {a.prenom}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Salle de réunion</label>
                  <select
                    value={createData.bureauId}
                    onChange={(e) => setCreateData((p) => ({ ...p, bureauId: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:bg-white focus:outline-none"
                    required
                  >
                    <option value="">Sélectionner...</option>
                    {bureaux.map((b) => (
                      <option key={b.idBureau} value={b.idBureau}>
                        N° {b.numero} - {b.type}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Date/Heure de Début</label>
                  <input
                    type="datetime-local"
                    value={createData.dateDebut}
                    onChange={(e) => setCreateData((p) => ({ ...p, dateDebut: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Date/Heure de Fin</label>
                  <input
                    type="datetime-local"
                    value={createData.dateFin}
                    onChange={(e) => setCreateData((p) => ({ ...p, dateFin: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-text-secondary uppercase">Motif de la réservation</label>
                <textarea
                  value={createData.motif}
                  onChange={(e) => setCreateData((p) => ({ ...p, motif: e.target.value }))}
                  className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  rows="3"
                  placeholder="Ex: Comité de direction"
                ></textarea>
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
                  Réserver
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Reservations;
