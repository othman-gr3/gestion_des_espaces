import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const Sites = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Pagination & Search state
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Modal form state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentSite, setCurrentSite] = useState(null); // null means "Create", otherwise "Edit"
  const [formData, setFormData] = useState({
    nom: '',
    code: '',
    rue: '',
    ville: '',
    codePostal: '',
    pays: 'France',
    image: '',
  });
  const [formError, setFormError] = useState('');

  useEffect(() => {
    fetchSites();
  }, [page, searchText]);

  const fetchSites = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/sites', {
        params: {
          searchText: searchText || undefined,
          pageNumber: page,
          pageSize: pageSize,
        },
      });
      setSites(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load sites:', err);
      setError('Impossible de charger la liste des sites.');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (site = null) => {
    setFormError('');
    if (site) {
      setCurrentSite(site);
      setFormData({
        nom: site.nom,
        code: site.code,
        rue: site.rue,
        ville: site.ville,
        codePostal: site.codePostal,
        pays: site.pays,
        image: site.image || '',
      });
    } else {
      setCurrentSite(null);
      setFormData({
        nom: '',
        code: '',
        rue: '',
        ville: '',
        codePostal: '',
        pays: 'France',
        image: '',
      });
    }
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
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
        // UPDATE — includes concurrency token round-trip
        await api.put(`/sites/${currentSite.idSite}`, {
          concurrencyToken: currentSite.concurrencyToken,
          nom: formData.nom,
          code: formData.code,
          rue: formData.rue,
          ville: formData.ville,
          codePostal: formData.codePostal,
          pays: formData.pays,
          image: formData.image || null,
        });
      } else {
        // CREATE
        await api.post('/sites', formData);
      }
      setIsModalOpen(false);
      fetchSites();
    } catch (err) {
      console.error('Submit error:', err);
      const detail = err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.";
      setFormError(detail);
    }
  };

  const handleDelete = async (site) => {
    if (!window.confirm(`Êtes-vous sûr de vouloir supprimer le site "${site.nom}" ?`)) return;
    try {
      // DELETE request with body support (using custom config)
      await api.delete(`/sites/${site.idSite}`, {
        data: { concurrencyToken: site.concurrencyToken }
      });
      fetchSites();
    } catch (err) {
      console.error('Delete error:', err);
      const detail = err.response?.data?.detail || 'Impossible de supprimer ce site.';
      alert(detail);
    }
  };

  return (
    <div className="space-y-6">
      {/* Action Header */}
      <div className="flex items-center justify-between">
        <div className="flex max-w-sm flex-1 gap-2">
          <input
            type="text"
            placeholder="Rechercher un site..."
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
            Nouveau site
          </button>
        )}
      </div>

      {error && (
        <div className="rounded bg-danger/10 border border-danger/20 p-4 text-sm font-semibold text-danger">
          {error}
        </div>
      )}

      {/* Zebra Striped Table Container */}
      <div className="overflow-hidden border border-border-subtle bg-surface-bg rounded-lg shadow-sm">
        <table className="min-w-full divide-y divide-border-subtle">
          <thead className="bg-neutral-bg">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Code</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Nom</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Adresse</th>
              {isGestionnaire && (
                <th className="px-6 py-3 text-right text-xs font-bold uppercase tracking-wider text-text-secondary">Actions</th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr>
                <td colSpan={isGestionnaire ? 4 : 3} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Chargement des données...
                </td>
              </tr>
            ) : sites.length === 0 ? (
              <tr>
                <td colSpan={isGestionnaire ? 4 : 3} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Aucun site trouvé.
                </td>
              </tr>
            ) : (
              sites.map((site, index) => (
                <tr key={site.idSite} className={index % 2 === 0 ? 'bg-surface-bg' : 'bg-neutral-bg/50'}>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-bold text-primary">{site.code}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-text-primary">{site.nom}</td>
                  <td className="px-6 py-4 text-sm text-text-secondary">
                    {site.rue}, {site.codePostal} {site.ville}, {site.pays}
                  </td>
                  {isGestionnaire && (
                    <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-medium space-x-3">
                      <button
                        onClick={() => handleOpenModal(site)}
                        className="text-primary hover:text-primary-dark transition-colors"
                      >
                        Modifier
                      </button>
                      <button
                        onClick={() => handleDelete(site)}
                        className="text-danger hover:text-red-700 transition-colors"
                      >
                        Supprimer
                      </button>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination Controls */}
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

      {/* Modal Dialog Form */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-lg border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-lg">
            <h3 className="text-lg font-bold text-text-primary mb-4">
              {currentSite ? 'Modifier le site' : 'Ajouter un nouveau site'}
            </h3>

            {formError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Nom du site</label>
                  <input
                    type="text"
                    name="nom"
                    value={formData.nom}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Code Unique</label>
                  <input
                    type="text"
                    name="code"
                    value={formData.code}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div>
                <label className="block text-xs font-bold text-text-secondary uppercase">Rue</label>
                <input
                  type="text"
                  name="rue"
                  value={formData.rue}
                  onChange={handleInputChange}
                  className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  required
                />
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div className="col-span-2">
                  <label className="block text-xs font-bold text-text-secondary uppercase">Ville</label>
                  <input
                    type="text"
                    name="ville"
                    value={formData.ville}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Code Postal</label>
                  <input
                    type="text"
                    name="codePostal"
                    value={formData.codePostal}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Pays</label>
                  <input
                    type="text"
                    name="pays"
                    value={formData.pays}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Image URL (optionnel)</label>
                  <input
                    type="text"
                    name="image"
                    value={formData.image}
                    onChange={handleInputChange}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div className="flex items-center justify-end gap-3 pt-4 border-t border-border-subtle">
                <button
                  type="button"
                  onClick={handleCloseModal}
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
    </div>
  );
};

export default Sites;
