import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import StatusBadge from '../components/StatusBadge';
import Pagination from '../components/Pagination';

const TypeConfig = {
  0: 'Changement de bureau',
  1: 'Problème avec un bureau',
  2: 'Problème avec un actif',
  3: 'Autre',
};

const StatutConfig = {
  0: { label: 'En attente', tone: 'warning' },
  1: { label: 'En cours', tone: 'neutral' },
  2: { label: 'Résolue', tone: 'success' },
  3: { label: 'Rejetée', tone: 'danger' },
};

const Demandes = () => {
  const [demandes, setDemandes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [statutFilter, setStatutFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentDemande, setCurrentDemande] = useState(null);
  const [reponse, setReponse] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [formError, setFormError] = useState('');

  useEffect(() => { fetchDemandes(); }, [page, statutFilter]);

  const fetchDemandes = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/demandes', {
        params: { statut: statutFilter !== '' ? parseInt(statutFilter, 10) : undefined, pageNumber: page, pageSize },
      });
      setDemandes(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load demandes:', err);
      setError('Impossible de charger les demandes.');
    } finally { setLoading(false); }
  };

  const handlePrendreEnCharge = async (demande) => {
    try {
      await api.post(`/demandes/${demande.idDemande}/prendre-en-charge`, { concurrencyToken: demande.concurrencyToken });
      fetchDemandes();
    } catch (err) {
      console.error('Take-charge error:', err);
      alert(err.response?.data?.detail || "Impossible de prendre en charge cette demande.");
    }
  };

  const handleOpenTreat = (demande) => {
    setCurrentDemande(demande);
    setReponse('');
    setFormError('');
    setIsDrawerOpen(true);
  };

  const submitTreatment = async (endpoint) => {
    setFormError('');
    if (!reponse.trim()) { setFormError('Veuillez saisir une réponse.'); return; }
    setSubmitting(true);
    try {
      await api.post(`/demandes/${currentDemande.idDemande}/${endpoint}`, {
        concurrencyToken: currentDemande.concurrencyToken,
        reponse,
      });
      setIsDrawerOpen(false);
      fetchDemandes();
    } catch (err) {
      console.error('Treatment error:', err);
      setFormError(err.response?.data?.detail || "L'opération a échoué.");
    } finally { setSubmitting(false); }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Demandes' }]} />

      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div>
            <label className="field-label">Statut</label>
            <select value={statutFilter} onChange={(e) => { setStatutFilter(e.target.value); setPage(1); }} className="form-field">
              <option value="">Tous les statuts</option>
              <option value="0">En attente</option>
              <option value="1">En cours</option>
              <option value="2">Résolue</option>
              <option value="3">Rejetée</option>
            </select>
          </div>
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Agent</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Type</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Description</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Envoyée le</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
              <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des demandes...</td></tr>
            ) : demandes.length === 0 ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucune demande ne correspond aux filtres.</td></tr>
            ) : (
              demandes.map((d) => {
                const st = StatutConfig[d.statut] || { label: 'Inconnu', tone: 'neutral' };
                const isClosed = d.statut === 2 || d.statut === 3;
                return (
                  <tr key={d.idDemande} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{d.agentNomComplet}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{TypeConfig[d.type] ?? '—'}</td>
                    <td className="px-4 py-2.5 text-[12.5px] text-text-secondary max-w-xs truncate" title={d.description}>{d.description}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(d.dateCreation).toLocaleDateString('fr-FR')}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={st.tone}>{st.label}</StatusBadge></td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-4">
                        {d.statut === 0 && (
                          <button onClick={() => handlePrendreEnCharge(d)} className="btn-text-action btn-text-action-primary">Prendre en charge</button>
                        )}
                        {!isClosed && (
                          <button onClick={() => handleOpenTreat(d)} className="btn-text-action btn-text-action-primary">Traiter</button>
                        )}
                        {isClosed && <span className="text-[12px] text-text-secondary italic">Clôturée</span>}
                      </div>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} />

      <Drawer
        open={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        eyebrow="Traitement"
        title="Traiter la demande"
      >
        {currentDemande && (
          <div className="p-6 space-y-5">
            <div>
              <div className="field-label">Agent</div>
              <div className="text-[13px] text-text-primary mt-1">{currentDemande.agentNomComplet}</div>
            </div>
            <div>
              <div className="field-label">Description</div>
              <div className="text-[13px] text-text-primary mt-1">{currentDemande.description}</div>
            </div>

            {formError && <div className="border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}

            <div>
              <label className="field-label">Réponse</label>
              <textarea
                value={reponse}
                onChange={(e) => setReponse(e.target.value)}
                className="form-field min-h-[80px] resize-none"
                placeholder="Expliquez la décision prise..."
                maxLength={1000}
                rows={3}
              />
            </div>

            <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
              <button type="button" onClick={() => setIsDrawerOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
              <button
                type="button"
                onClick={() => submitTreatment('rejeter')}
                disabled={submitting}
                className="border border-danger px-5 py-2 text-[11.5px] font-semibold uppercase tracking-wider text-danger hover:bg-danger/5 transition-colors disabled:opacity-50"
                style={{ fontFamily: 'var(--font-mono)' }}
              >
                Rejeter
              </button>
              <button
                type="button"
                onClick={() => submitTreatment('resoudre')}
                disabled={submitting}
                className="bg-primary px-5 py-2 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
                style={{ fontFamily: 'var(--font-mono)' }}
              >
                Résoudre
              </button>
            </div>
          </div>
        )}
      </Drawer>
    </div>
  );
};

export default Demandes;
