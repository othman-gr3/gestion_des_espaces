import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const StatutLabels = {
  0: { label: 'Disponible', class: 'bg-success/10 text-success border border-success/20' },
  1: { label: 'En Maintenance', class: 'bg-warning/10 text-warning border border-warning/20' },
  2: { label: 'Hors Service', class: 'bg-danger/10 text-danger border border-danger/20' },
};

const Spaces = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [bureaux, setBureaux] = useState([]);
  const [batiments, setBatiments] = useState([]);
  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filters state
  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [selectedBatimentId, setSelectedBatimentId] = useState('');
  const [statutFilter, setStatutFilter] = useState('');
  const [searchText, setSearchText] = useState('');

  // Pagination state
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Form modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentBureau, setCurrentBureau] = useState(null);
  const [formData, setFormData] = useState({
    numero: '',
    type: 'Bureau individuel',
    capacite: 1,
    superficie: 10,
    etage: 0,
    image: '',
    idBatiment: '',
    statut: 0,
  });
  const [formError, setFormError] = useState('');

  useEffect(() => {
    fetchSitesAndBatiments();
  }, []);

  useEffect(() => {
    fetchBureaux();
  }, [page, selectedBatimentId, statutFilter, searchText]);

  const fetchSitesAndBatiments = async () => {
    try {
      const [sitesRes, batimentsRes] = await Promise.all([
        api.get('/sites?pageSize=100'),
        api.get('/batiments?pageSize=100'),
      ]);
      setSites(sitesRes.data.items || []);
      setBatiments(batimentsRes.data.items || []);
    } catch (err) {
      console.error('Failed to load initial spaces data:', err);
    }
  };

  const fetchBureaux = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/bureaux', {
        params: {
          idBatiment: selectedBatimentId || undefined,
          searchText: searchText || undefined,
          statut: statutFilter !== '' ? parseInt(statutFilter) : undefined,
          pageNumber: page,
          pageSize: pageSize,
        },
      });
      setBureaux(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bureaux:', err);
      setError('Impossible de charger les espaces.');
    } finally {
      setLoading(false);
    }
  };

  // Filter buildings by selected site
  const filteredBatiments = selectedSiteId
    ? batiments.filter((b) => b.idSite === parseInt(selectedSiteId))
    : batiments;

  const handleSiteChange = (e) => {
    setSelectedSiteId(e.target.value);
    setSelectedBatimentId('');
    setPage(1);
  };

  const handleOpenModal = (bureau = null) => {
    setFormError('');
    if (bureau) {
      setCurrentBureau(bureau);
      setFormData({
        numero: bureau.numero,
        type: bureau.type || 'Bureau individuel',
        capacite: bureau.capacite,
        superficie: bureau.superficie,
        etage: bureau.etage,
        image: bureau.image || '',
        idBatiment: bureau.idBatiment,
        statut: bureau.statut,
      });
    } else {
      setCurrentBureau(null);
      setFormData({
        numero: '',
        type: 'Bureau individuel',
        capacite: 1,
        superficie: 10,
        etage: 0,
        image: '',
        idBatiment: selectedBatimentId || (filteredBatiments[0]?.idBatiment || ''),
        statut: 0,
      });
    }
    setIsModalOpen(true);
  };

  const handleCloseModal = () => {
    setIsModalOpen(false);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      if (currentBureau) {
        await api.put(`/bureaux/${currentBureau.idBureau}`, {
          concurrencyToken: currentBureau.concurrencyToken,
          ...formData,
          capacite: parseInt(formData.capacite),
          superficie: parseFloat(formData.superficie),
          etage: parseInt(formData.etage),
          idBatiment: parseInt(formData.idBatiment),
          statut: parseInt(formData.statut),
        });
      } else {
        await api.post('/bureaux', {
          ...formData,
          capacite: parseInt(formData.capacite),
          superficie: parseFloat(formData.superficie),
          etage: parseInt(formData.etage),
          idBatiment: parseInt(formData.idBatiment),
          statut: parseInt(formData.statut),
        });
      }
      setIsModalOpen(false);
      fetchBureaux();
    } catch (err) {
      console.error('Submit error:', err);
      const detail = err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.";
      setFormError(detail);
    }
  };

  return (
    <div className="space-y-6">
      {/* Filters Toolbar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border border-border-subtle bg-surface-bg p-4 rounded-lg shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-4 flex-1">
          {/* Site Filter */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Site</label>
            <select
              value={selectedSiteId}
              onChange={handleSiteChange}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les sites</option>
              {sites.map((site) => (
                <option key={site.idSite} value={site.idSite}>
                  {site.nom}
                </option>
              ))}
            </select>
          </div>

          {/* Building Filter */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Bâtiment</label>
            <select
              value={selectedBatimentId}
              onChange={(e) => {
                setSelectedBatimentId(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les bâtiments</option>
              {filteredBatiments.map((b) => (
                <option key={b.idBatiment} value={b.idBatiment}>
                  {b.nom}
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
              <option value="0">Disponible</option>
              <option value="1">En Maintenance</option>
              <option value="2">Hors Service</option>
            </select>
          </div>

          {/* Text Search */}
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">N° Bureau</label>
            <input
              type="text"
              placeholder="Ex: A-102"
              value={searchText}
              onChange={(e) => {
                setSearchText(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2.5 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            />
          </div>
        </div>

        {isGestionnaire && (
          <button
            onClick={() => handleOpenModal()}
            className="self-end rounded bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none"
          >
            Nouvel Espace
          </button>
        )}
      </div>

      {error && (
        <div className="rounded bg-danger/10 border border-danger/20 p-4 text-sm font-semibold text-danger">
          {error}
        </div>
      )}

      {/* Zebra Striped Space Table */}
      <div className="overflow-hidden border border-border-subtle bg-surface-bg rounded-lg shadow-sm">
        <table className="min-w-full divide-y divide-border-subtle">
          <thead className="bg-neutral-bg">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Numéro</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Type</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Capacité</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Superficie</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Étage</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Statut</th>
              {isGestionnaire && (
                <th className="px-6 py-3 text-right text-xs font-bold uppercase tracking-wider text-text-secondary">Actions</th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr>
                <td colSpan={isGestionnaire ? 7 : 6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Chargement des bureaux...
                </td>
              </tr>
            ) : bureaux.length === 0 ? (
              <tr>
                <td colSpan={isGestionnaire ? 7 : 6} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Aucun espace trouvé.
                </td>
              </tr>
            ) : (
              bureaux.map((b, index) => (
                <tr key={b.idBureau} className={index % 2 === 0 ? 'bg-surface-bg' : 'bg-neutral-bg/50'}>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-bold text-primary">{b.numero}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">{b.type}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">{b.capacite} poste(s)</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-secondary">{b.superficie} m²</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-secondary">{b.etage}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm">
                    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${StatutLabels[b.statut]?.class || ''}`}>
                      {StatutLabels[b.statut]?.label || 'Inconnu'}
                    </span>
                  </td>
                  {isGestionnaire && (
                    <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-medium">
                      <button
                        onClick={() => handleOpenModal(b)}
                        className="text-primary hover:text-primary-dark transition-colors"
                      >
                        Modifier
                      </button>
                    </td>
                  )}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Pagination controls */}
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
              {currentBureau ? "Modifier l'espace" : 'Ajouter un espace'}
            </h3>

            {formError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Numéro</label>
                  <input
                    type="text"
                    value={formData.numero}
                    onChange={(e) => setFormData((p) => ({ ...p, numero: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Type d'espace</label>
                  <input
                    type="text"
                    value={formData.type}
                    onChange={(e) => setFormData((p) => ({ ...p, type: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Capacité</label>
                  <input
                    type="number"
                    value={formData.capacite}
                    onChange={(e) => setFormData((p) => ({ ...p, capacite: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    min="1"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Superficie (m²)</label>
                  <input
                    type="number"
                    value={formData.superficie}
                    onChange={(e) => setFormData((p) => ({ ...p, superficie: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    min="1"
                    step="0.1"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Étage</label>
                  <input
                    type="number"
                    value={formData.etage}
                    onChange={(e) => setFormData((p) => ({ ...p, etage: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Bâtiment parent</label>
                  <select
                    value={formData.idBatiment}
                    onChange={(e) => setFormData((p) => ({ ...p, idBatiment: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  >
                    <option value="">Sélectionner...</option>
                    {batiments.map((b) => (
                      <option key={b.idBatiment} value={b.idBatiment}>
                        {b.nom}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Statut initial</label>
                  <select
                    value={formData.statut}
                    onChange={(e) => setFormData((p) => ({ ...p, statut: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  >
                    <option value="0">Disponible</option>
                    <option value="1">En Maintenance</option>
                    <option value="2">Hors Service</option>
                  </select>
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

export default Spaces;
