import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Sites = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentSite, setCurrentSite] = useState(null);
  const [formData, setFormData] = useState({ nom: '', code: '', rue: '', ville: '', codePostal: '', pays: 'France', image: '' });
  const [formError, setFormError] = useState('');

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

  const handleOpenModal = (site = null) => {
    setFormError('');
    if (site) {
      setCurrentSite(site);
      setFormData({ nom: site.nom, code: site.code, rue: site.rue, ville: site.ville, codePostal: site.codePostal, pays: site.pays, image: site.image || '' });
    } else {
      setCurrentSite(null);
      setFormData({ nom: '', code: '', rue: '', ville: '', codePostal: '', pays: 'France', image: '' });
    }
    setIsModalOpen(true);
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
      setIsModalOpen(false);
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
      {/* Toolbar */}
      <div className="flex items-center justify-between border-b-2 border-primary bg-surface-bg px-5 py-3 mb-6">
        <div className="flex items-center gap-3 flex-1 max-w-xs">
          <input
            type="text"
            placeholder="Rechercher un site..."
            value={searchText}
            onChange={(e) => { setSearchText(e.target.value); setPage(1); }}
            className="form-field flex-1"
          />
        </div>
        {isGestionnaire && (
          <button
            onClick={() => handleOpenModal()}
            className="ml-4 bg-primary px-5 py-2 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors"
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
      <div className="border border-border-subtle bg-surface-bg overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-6 py-3 text-left"><span className="th-label">Code</span></th>
              <th className="px-6 py-3 text-left"><span className="th-label">Nom du site</span></th>
              <th className="px-6 py-3 text-left"><span className="th-label">Adresse</span></th>
              {isGestionnaire && <th className="px-6 py-3 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isGestionnaire ? 4 : 3} className="px-6 py-10 text-center text-[13px] text-text-secondary">Chargement des données...</td></tr>
            ) : sites.length === 0 ? (
              <tr><td colSpan={isGestionnaire ? 4 : 3} className="px-6 py-10 text-center text-[13px] text-text-secondary">Aucun site ne correspond à la recherche.</td></tr>
            ) : (
              sites.map((site) => (
                <tr key={site.idSite} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-6 py-4 text-[13px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{site.code}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-[14px] font-medium text-text-primary">{site.nom}</td>
                  <td className="px-6 py-4 text-[13px] text-text-secondary">{site.rue}, {site.codePostal} {site.ville}, {site.pays}</td>
                  {isGestionnaire && (
                    <td className="whitespace-nowrap px-6 py-4 text-right">
                      <div className="flex items-center justify-end gap-5">
                        <button onClick={() => handleOpenModal(site)} className="btn-text-action btn-text-action-primary">Modifier</button>
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

      {/* Pagination */}
      {totalCount > pageSize && (
        <div className="flex items-center justify-between border-t border-border-subtle pt-4 mt-4">
          <button disabled={page === 1} onClick={() => setPage((p) => Math.max(p - 1, 1))} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40 transition-colors" style={{ fontFamily: 'var(--font-sans)' }}>← Précédent</button>
          <span className="text-[12px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>Page {page} / {Math.ceil(totalCount / pageSize)}</span>
          <button disabled={page * pageSize >= totalCount} onClick={() => setPage((p) => p + 1)} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40 transition-colors" style={{ fontFamily: 'var(--font-sans)' }}>Suivant →</button>
        </div>
      )}

      {/* Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setIsModalOpen(false)}>
          <div className="modal-slide-in h-full w-full max-w-lg bg-surface-bg border-l-2 border-primary overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between border-b-2 border-primary px-6 py-4 bg-neutral-bg">
              <div>
                <div className="th-label mb-0.5">{currentSite ? 'Modification' : 'Création'}</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>{currentSite ? 'Modifier le site' : 'Nouveau site'}</h3>
              </div>
              <button onClick={() => setIsModalOpen(false)} className="text-text-secondary hover:text-text-primary transition-colors text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>

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
              <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
                <button type="button" onClick={() => setIsModalOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
                <button type="submit" className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>Enregistrer</button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Sites;
