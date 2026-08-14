import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import Pagination from '../components/Pagination';
import SortableTh from '../components/SortableTh';
import useSort from '../hooks/useSort';

const getSortValue = (agent, col) => {
  if (col === 'nom') return `${agent.nom} ${agent.prenom}`;
  return agent[col];
};

const Agents = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Administrateur');

  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentAgent, setCurrentAgent] = useState(null);
  const [formData, setFormData] = useState({ nom: '', prenom: '', matricule: '', email: '', telephone: '', fonction: '', departement: '', dateEmbauche: '', image: '' });
  const [formError, setFormError] = useState('');

  const { sortedRows, sortKey, sortDir, onSort } = useSort(agents, getSortValue, 'nom');

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

  const handleOpenDrawer = (agent = null) => {
    setFormError('');
    if (agent) {
      setCurrentAgent(agent);
      setFormData({ nom: agent.nom, prenom: agent.prenom, matricule: agent.matricule, email: agent.email || '', telephone: agent.telephone || '', fonction: agent.fonction || '', departement: agent.departement || '', dateEmbauche: agent.dateEmbauche ? agent.dateEmbauche.split('T')[0] : '', image: agent.image || '' });
    } else {
      setCurrentAgent(null);
      setFormData({ nom: '', prenom: '', matricule: '', email: '', telephone: '', fonction: '', departement: '', dateEmbauche: '', image: '' });
    }
    setIsDrawerOpen(true);
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
      setIsDrawerOpen(false);
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

  return (
    <div>
      <Breadcrumb items={[{ label: 'Référentiel' }, { label: 'Agents' }]} />

      {/* Toolbar */}
      <div className="flex items-center justify-between border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="flex items-center gap-3 flex-1 max-w-sm">
          <input
            type="text"
            placeholder="Rechercher nom, prénom, matricule..."
            value={searchText}
            onChange={(e) => { setSearchText(e.target.value); setPage(1); }}
            className="form-field flex-1"
          />
        </div>
        {isAdmin && (
          <button onClick={() => handleOpenDrawer()} className="ml-4 bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
            + Nouvel agent
          </button>
        )}
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <SortableTh label="Matricule" column="matricule" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Nom / Prénom" column="nom" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Email / Tél.</span></th>
              <SortableTh label="Département" column="departement" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              {isAdmin && <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isAdmin ? 5 : 4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des agents...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={isAdmin ? 5 : 4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun agent trouvé.</td></tr>
            ) : (
              sortedRows.map((agent) => (
                <tr key={agent.idAgent} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{agent.matricule}</td>
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <div className="text-[13px] font-semibold text-text-primary">{agent.nom.toUpperCase()} {agent.prenom}</div>
                    {agent.fonction && <div className="text-[11.5px] text-text-secondary mt-0.5">{agent.fonction}</div>}
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <div className="text-[12.5px] text-text-secondary">{agent.email || '—'}</div>
                    {agent.telephone && <div className="text-[11.5px] text-text-secondary opacity-75">{agent.telephone}</div>}
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{agent.departement || '—'}</td>
                  {isAdmin && (
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-4">
                        <button onClick={() => handleOpenDrawer(agent)} className="btn-text-action btn-text-action-primary">Modifier</button>
                        <button onClick={() => handleDelete(agent)} className="btn-text-action btn-text-action-danger">Supprimer</button>
                      </div>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} />

      <Drawer
        open={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        eyebrow={currentAgent ? 'Modification' : 'Création'}
        title={currentAgent ? 'Fiche agent' : 'Nouvel agent'}
      >
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
            <button type="button" onClick={() => setIsDrawerOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
            <button type="submit" className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>Enregistrer</button>
          </div>
        </form>
      </Drawer>
    </div>
  );
};

export default Agents;
