import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import Pagination from '../components/Pagination';
import SortableTh from '../components/SortableTh';
import useSort from '../hooks/useSort';

const getSortValue = (batiment, col) => batiment[col];

const Batiments = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Administrateur');

  const [batiments, setBatiments] = useState([]);
  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentBatiment, setCurrentBatiment] = useState(null);
  const [formData, setFormData] = useState({ nom: '', nombreEtages: 1, superficie: 100, image: '', idSite: '' });
  const [formError, setFormError] = useState('');

  const { sortedRows, sortKey, sortDir, onSort } = useSort(batiments, getSortValue, 'nom');

  useEffect(() => { fetchSites(); }, []);
  useEffect(() => { fetchBatiments(); }, [page, selectedSiteId, searchText]);

  const fetchSites = async () => {
    try {
      const res = await api.get('/sites?pageSize=100');
      setSites(res.data.items || []);
    } catch (err) { console.error('Failed to load sites:', err); }
  };

  const fetchBatiments = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/batiments', {
        params: { idSite: selectedSiteId || undefined, searchText: searchText || undefined, pageNumber: page, pageSize },
      });
      setBatiments(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load batiments:', err);
      setError('Impossible de charger la liste des bâtiments.');
    } finally { setLoading(false); }
  };

  const siteName = (idSite) => sites.find((s) => s.idSite === idSite)?.nom || `Site ${idSite}`;

  const handleOpenDrawer = (batiment = null) => {
    setFormError('');
    if (batiment) {
      setCurrentBatiment(batiment);
      setFormData({ nom: batiment.nom, nombreEtages: batiment.nombreEtages, superficie: batiment.superficie, image: batiment.image || '', idSite: batiment.idSite });
    } else {
      setCurrentBatiment(null);
      setFormData({ nom: '', nombreEtages: 1, superficie: 100, image: '', idSite: selectedSiteId || (sites[0]?.idSite || '') });
    }
    setIsDrawerOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      const payload = { ...formData, nombreEtages: parseInt(formData.nombreEtages), superficie: parseFloat(formData.superficie), idSite: parseInt(formData.idSite) };
      if (currentBatiment) {
        await api.put(`/batiments/${currentBatiment.idBatiment}`, { concurrencyToken: currentBatiment.concurrencyToken, ...payload });
      } else {
        await api.post('/batiments', payload);
      }
      setIsDrawerOpen(false);
      fetchBatiments();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  const handleDelete = async (batiment) => {
    if (!window.confirm(`Supprimer le bâtiment "${batiment.nom}" ?`)) return;
    try {
      await api.delete(`/batiments/${batiment.idBatiment}`, { data: { concurrencyToken: batiment.concurrencyToken } });
      fetchBatiments();
    } catch (err) {
      console.error('Delete error:', err);
      alert(err.response?.data?.detail || 'Impossible de supprimer ce bâtiment.');
    }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Référentiel' }, { label: 'Bâtiments' }]} />

      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 flex-1 max-w-xl">
            <div>
              <label className="field-label">Site</label>
              <select value={selectedSiteId} onChange={(e) => { setSelectedSiteId(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous les sites</option>
                {sites.map((s) => <option key={s.idSite} value={s.idSite}>{s.nom}</option>)}
              </select>
            </div>
            <div className="col-span-2 sm:col-span-2">
              <label className="field-label">Recherche</label>
              <input type="text" placeholder="Nom du bâtiment..." value={searchText} onChange={(e) => { setSearchText(e.target.value); setPage(1); }} className="form-field" />
            </div>
          </div>
          {isAdmin && (
            <button onClick={() => handleOpenDrawer()} className="sm:ml-4 self-end bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors flex-shrink-0" style={{ fontFamily: 'var(--font-mono)' }}>
              + Nouveau bâtiment
            </button>
          )}
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <SortableTh label="Bâtiment" column="nom" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Site</span></th>
              <SortableTh label="Étages" column="nombreEtages" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Superficie" column="superficie" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Bureaux</span></th>
              {isAdmin && <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isAdmin ? 6 : 5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des bâtiments...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={isAdmin ? 6 : 5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun bâtiment ne correspond aux filtres.</td></tr>
            ) : (
              sortedRows.map((b) => (
                <tr key={b.idBatiment} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-semibold text-text-primary">{b.nom}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{siteName(b.idSite)}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{b.nombreEtages}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{b.superficie} m²</td>
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <Link to={`/bureaux?idBatiment=${b.idBatiment}`} className="btn-text-action btn-text-action-primary">Voir les bureaux →</Link>
                  </td>
                  {isAdmin && (
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-4">
                        <button onClick={() => handleOpenDrawer(b)} className="btn-text-action btn-text-action-primary">Modifier</button>
                        <button onClick={() => handleDelete(b)} className="btn-text-action btn-text-action-danger">Supprimer</button>
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
        eyebrow={currentBatiment ? 'Modification' : 'Création'}
        title={currentBatiment ? 'Modifier le bâtiment' : 'Nouveau bâtiment'}
      >
        {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <div>
            <label className="field-label">Nom du bâtiment</label>
            <input type="text" value={formData.nom} onChange={(e) => setFormData((p) => ({ ...p, nom: e.target.value }))} className="form-field" required />
          </div>
          <div>
            <label className="field-label">Site parent</label>
            <select value={formData.idSite} onChange={(e) => setFormData((p) => ({ ...p, idSite: e.target.value }))} className="form-field" required>
              <option value="">Sélectionner...</option>
              {sites.map((s) => <option key={s.idSite} value={s.idSite}>{s.nom}</option>)}
            </select>
          </div>
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Nombre d'étages</label>
              <input type="number" value={formData.nombreEtages} onChange={(e) => setFormData((p) => ({ ...p, nombreEtages: e.target.value }))} className="form-field" min="0" required />
            </div>
            <div>
              <label className="field-label">Superficie (m²)</label>
              <input type="number" value={formData.superficie} onChange={(e) => setFormData((p) => ({ ...p, superficie: e.target.value }))} className="form-field" min="1" step="0.1" required />
            </div>
          </div>
          <div>
            <label className="field-label">Image URL (optionnel)</label>
            <input type="text" value={formData.image} onChange={(e) => setFormData((p) => ({ ...p, image: e.target.value }))} className="form-field" />
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

export default Batiments;
