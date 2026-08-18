import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import Pagination from '../components/Pagination';
import SortableTh from '../components/SortableTh';
import useSort from '../hooks/useSort';

const getSortValue = (site, col) => {
  if (col === 'adresse') return `${site.ville} ${site.rue}`;
  return site[col];
};

const Sites = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Administrateur');

  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentSite, setCurrentSite] = useState(null);
  const [formData, setFormData] = useState({ nom: '', code: '', rue: '', ville: '', codePostal: '', pays: 'France', telephone: '', email: '', image: '' });
  const [formError, setFormError] = useState('');

  const { sortedRows, sortKey, sortDir, onSort } = useSort(sites, getSortValue, 'nom');

  useEffect(() => { fetchSites(); }, [page, searchText]);

  const fetchSites = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/sites', { params: { searchText: searchText || undefined, pageNumber: page, pageSize } });
      setSites(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load sites:', err);
      setError('Impossible de charger la liste des sites.');
    } finally { setLoading(false); }
  };

  const handleOpenDrawer = (site = null) => {
    setFormError('');
    if (site) {
      setCurrentSite(site);
      setFormData({ nom: site.nom, code: site.code, rue: site.rue, ville: site.ville, codePostal: site.codePostal, pays: site.pays, telephone: site.telephone || '', email: site.email || '', image: site.image || '' });
    } else {
      setCurrentSite(null);
      setFormData({ nom: '', code: '', rue: '', ville: '', codePostal: '', pays: 'France', telephone: '', email: '', image: '' });
    }
    setIsDrawerOpen(true);
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      if (currentSite) {
        await api.put(`/sites/${currentSite.idSite}`, { concurrencyToken: currentSite.concurrencyToken, ...formData });
      } else {
        await api.post('/sites', formData);
      }
      setIsDrawerOpen(false);
      fetchSites();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  const handleDelete = async (site) => {
    if (!window.confirm(`Supprimer le site "${site.nom}" ?`)) return;
    try {
      await api.delete(`/sites/${site.idSite}`, { data: { concurrencyToken: site.concurrencyToken } });
      fetchSites();
    } catch (err) {
      console.error('Delete error:', err);
      alert(err.response?.data?.detail || 'Impossible de supprimer ce site.');
    }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Référentiel' }, { label: 'Sites' }]} />

      {/* Toolbar */}
      <div className="flex items-center justify-between border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="flex items-center gap-3 flex-1 max-w-xs">
          <input
            type="text"
            placeholder="Rechercher un site..."
            value={searchText}
            onChange={(e) => { setSearchText(e.target.value); setPage(1); }}
            className="form-field flex-1"
          />
        </div>
        {isAdmin && (
          <button
            onClick={() => handleOpenDrawer()}
            className="ml-4 bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            + Nouveau site
          </button>
        )}
      </div>

      {error && (
        <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>
      )}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <SortableTh label="Code" column="code" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Nom du site" column="nom" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Adresse" column="adresse" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Contact</span></th>
              {isAdmin && <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isAdmin ? 5 : 4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des données...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={isAdmin ? 5 : 4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun site ne correspond à la recherche.</td></tr>
            ) : (
              sortedRows.map((site) => (
                <tr key={site.idSite} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{site.code}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{site.nom}</td>
                  <td className="px-4 py-2.5 text-[12.5px] text-text-secondary">{site.rue}, {site.codePostal} {site.ville}, {site.pays}</td>
                  <td className="px-4 py-2.5 text-[12.5px] text-text-secondary">
                    {site.telephone && <div>{site.telephone}</div>}
                    {site.email && <div>{site.email}</div>}
                    {!site.telephone && !site.email && '—'}
                  </td>
                  {isAdmin && (
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      <div className="flex items-center justify-end gap-4">
                        <button onClick={() => handleOpenDrawer(site)} className="btn-text-action btn-text-action-primary">Modifier</button>
                        <button onClick={() => handleDelete(site)} className="btn-text-action btn-text-action-danger">Supprimer</button>
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
        eyebrow={currentSite ? 'Modification' : 'Création'}
        title={currentSite ? 'Modifier le site' : 'Nouveau site'}
      >
        {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Nom du site</label>
              <input type="text" name="nom" value={formData.nom} onChange={handleInputChange} className="form-field" required />
            </div>
            <div>
              <label className="field-label">Code unique</label>
              <input type="text" name="code" value={formData.code} onChange={handleInputChange} className="form-field" required />
            </div>
          </div>
          <div>
            <label className="field-label">Rue</label>
            <input type="text" name="rue" value={formData.rue} onChange={handleInputChange} className="form-field" required />
          </div>
          <div className="grid grid-cols-3 gap-5">
            <div className="col-span-2">
              <label className="field-label">Ville</label>
              <input type="text" name="ville" value={formData.ville} onChange={handleInputChange} className="form-field" required />
            </div>
            <div>
              <label className="field-label">Code postal</label>
              <input type="text" name="codePostal" value={formData.codePostal} onChange={handleInputChange} className="form-field" required />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Pays</label>
              <input type="text" name="pays" value={formData.pays} onChange={handleInputChange} className="form-field" required />
            </div>
            <div>
              <label className="field-label">Image URL (optionnel)</label>
              <input type="text" name="image" value={formData.image} onChange={handleInputChange} className="form-field" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Téléphone (optionnel)</label>
              <input type="text" name="telephone" value={formData.telephone} onChange={handleInputChange} className="form-field" maxLength={20} />
            </div>
            <div>
              <label className="field-label">Email (optionnel)</label>
              <input type="email" name="email" value={formData.email} onChange={handleInputChange} className="form-field" maxLength={100} />
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

export default Sites;
