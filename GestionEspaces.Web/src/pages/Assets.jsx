import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const EtatLabels = {
  0: { label: 'Neuf', class: 'bg-success/10 text-success border border-success/20' },
  1: { label: 'Bon', class: 'bg-success/10 text-success border border-success/20' },
  2: { label: 'À Réparer', class: 'bg-warning/10 text-warning border border-warning/20' },
  3: { label: 'Hors Service', class: 'bg-danger/10 text-danger border border-danger/20' },
};

const Assets = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [actifs, setActifs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  // Filtering state
  const [searchText, setSearchText] = useState('');
  const [etatFilter, setEtatFilter] = useState('');

  // Pagination state
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  // Form modal state
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentActif, setCurrentActif] = useState(null);
  const [formData, setFormData] = useState({
    nom: '',
    type: 'Ordinateur',
    marque: '',
    modele: '',
    numeroSerie: '',
    dateAchat: '',
    image: '',
    etat: 0,
  });
  const [formError, setFormError] = useState('');

  useEffect(() => {
    fetchActifs();
  }, [page, searchText, etatFilter]);

  const fetchActifs = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/actifs', {
        params: {
          searchText: searchText || undefined,
          etat: etatFilter !== '' ? parseInt(etatFilter) : undefined,
          pageNumber: page,
          pageSize: pageSize,
        },
      });
      setActifs(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load assets:', err);
      setError('Impossible de charger la liste des matériels.');
    } finally {
      setLoading(false);
    }
  };

  const handleOpenModal = (actif = null) => {
    setFormError('');
    if (actif) {
      setCurrentActif(actif);
      setFormData({
        nom: actif.nom,
        type: actif.type || '',
        marque: actif.marque || '',
        modele: actif.modele || '',
        numeroSerie: actif.numeroSerie || '',
        dateAchat: actif.dateAchat ? actif.dateAchat.split('T')[0] : '',
        image: actif.image || '',
        etat: actif.etat,
      });
    } else {
      setCurrentActif(null);
      setFormData({
        nom: '',
        type: 'Ordinateur',
        marque: '',
        modele: '',
        numeroSerie: '',
        dateAchat: '',
        image: '',
        etat: 0,
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
      const payload = {
        ...formData,
        dateAchat: formData.dateAchat ? new Date(formData.dateAchat).toISOString() : null,
        etat: parseInt(formData.etat),
      };

      if (currentActif) {
        await api.put(`/actifs/${currentActif.idActif}`, {
          concurrencyToken: currentActif.concurrencyToken,
          ...payload,
        });
      } else {
        await api.post('/actifs', payload);
      }
      setIsModalOpen(false);
      fetchActifs();
    } catch (err) {
      console.error('Submit error:', err);
      const detail = err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.";
      setFormError(detail);
    }
  };

  const handleDelete = async (actif) => {
    if (!window.confirm(`Êtes-vous sûr de vouloir supprimer le matériel "${actif.nom}" ?`)) return;
    try {
      await api.delete(`/actifs/${actif.idActif}`, {
        data: { concurrencyToken: actif.concurrencyToken }
      });
      fetchActifs();
    } catch (err) {
      console.error('Delete error:', err);
      const detail = err.response?.data?.detail || 'Impossible de supprimer ce matériel.';
      alert(detail);
    }
  };

  return (
    <div className="space-y-6">
      {/* Search and Action toolbar */}
      <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between border border-border-subtle bg-surface-bg p-4 rounded-lg shadow-sm">
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3 flex-1">
          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">Recherche</label>
            <input
              type="text"
              placeholder="Rechercher nom, n° de série..."
              value={searchText}
              onChange={(e) => {
                setSearchText(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2.5 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            />
          </div>

          <div>
            <label className="block text-[10px] font-bold text-text-secondary uppercase tracking-wider mb-1">État matériel</label>
            <select
              value={etatFilter}
              onChange={(e) => {
                setEtatFilter(e.target.value);
                setPage(1);
              }}
              className="w-full rounded border border-border-subtle bg-neutral-bg px-2 py-1.5 text-xs text-text-primary focus:bg-white focus:outline-none"
            >
              <option value="">Tous les états</option>
              <option value="0">Neuf</option>
              <option value="1">Bon</option>
              <option value="2">À Réparer</option>
              <option value="3">Hors Service</option>
            </select>
          </div>
        </div>

        {isGestionnaire && (
          <button
            onClick={() => handleOpenModal()}
            className="self-end rounded bg-primary px-4 py-2 text-sm font-bold text-white hover:bg-primary-dark transition-colors focus:outline-none"
          >
            Nouvel Actif
          </button>
        )}
      </div>

      {error && (
        <div className="rounded bg-danger/10 border border-danger/20 p-4 text-sm font-semibold text-danger">
          {error}
        </div>
      )}

      {/* Zebra striped table */}
      <div className="overflow-hidden border border-border-subtle bg-surface-bg rounded-lg shadow-sm">
        <table className="min-w-full divide-y divide-border-subtle">
          <thead className="bg-neutral-bg">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Désignation</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Type</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">Marque / Modèle</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">N° de Série</th>
              <th className="px-6 py-3 text-left text-xs font-bold uppercase tracking-wider text-text-secondary">État</th>
              {isGestionnaire && (
                <th className="px-6 py-3 text-right text-xs font-bold uppercase tracking-wider text-text-secondary">Actions</th>
              )}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr>
                <td colSpan={isGestionnaire ? 6 : 5} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Chargement des matériels...
                </td>
              </tr>
            ) : actifs.length === 0 ? (
              <tr>
                <td colSpan={isGestionnaire ? 6 : 5} className="px-6 py-8 text-center text-sm text-text-secondary">
                  Aucun matériel trouvé.
                </td>
              </tr>
            ) : (
              actifs.map((a, index) => (
                <tr key={a.idActif} className={index % 2 === 0 ? 'bg-surface-bg' : 'bg-neutral-bg/50'}>
                  <td className="whitespace-nowrap px-6 py-4 text-sm font-bold text-primary">{a.nom}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">{a.type}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-primary">
                    {a.marque} {a.modele}
                  </td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm text-text-secondary">{a.numeroSerie || '-'}</td>
                  <td className="whitespace-nowrap px-6 py-4 text-sm">
                    <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-semibold ${EtatLabels[a.etat]?.class || ''}`}>
                      {EtatLabels[a.etat]?.label || 'Inconnu'}
                    </span>
                  </td>
                  {isGestionnaire && (
                    <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-medium space-x-3">
                      <button
                        onClick={() => handleOpenModal(a)}
                        className="text-primary hover:text-primary-dark transition-colors"
                      >
                        Modifier
                      </button>
                      <button
                        onClick={() => handleDelete(a)}
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

      {/* Modal Dialog Form */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 backdrop-blur-xs p-4">
          <div className="w-full max-w-lg border border-border-subtle bg-surface-bg p-6 rounded-lg shadow-lg">
            <h3 className="text-lg font-bold text-text-primary mb-4">
              {currentActif ? 'Modifier le matériel' : 'Ajouter un matériel'}
            </h3>

            {formError && (
              <div className="mb-4 rounded bg-danger/10 border border-danger/20 p-3 text-xs font-semibold text-danger">
                {formError}
              </div>
            )}

            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <div>
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
                  <label className="block text-xs font-bold text-text-secondary uppercase">Type</label>
                  <input
                    type="text"
                    value={formData.type}
                    onChange={(e) => setFormData((p) => ({ ...p, type: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-3 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Marque</label>
                  <input
                    type="text"
                    value={formData.marque}
                    onChange={(e) => setFormData((p) => ({ ...p, marque: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Modèle</label>
                  <input
                    type="text"
                    value={formData.modele}
                    onChange={(e) => setFormData((p) => ({ ...p, modele: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">N° de Série</label>
                  <input
                    type="text"
                    value={formData.numeroSerie}
                    onChange={(e) => setFormData((p) => ({ ...p, numeroSerie: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">Date d'achat</label>
                  <input
                    type="date"
                    value={formData.dateAchat}
                    onChange={(e) => setFormData((p) => ({ ...p, dateAchat: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                  />
                </div>
                <div>
                  <label className="block text-xs font-bold text-text-secondary uppercase">État</label>
                  <select
                    value={formData.etat}
                    onChange={(e) => setFormData((p) => ({ ...p, etat: e.target.value }))}
                    className="mt-1.5 block w-full rounded border border-border-subtle bg-neutral-bg px-3 py-2 text-sm text-text-primary focus:border-primary focus:bg-white focus:outline-none"
                    required
                  >
                    <option value="0">Neuf</option>
                    <option value="1">Bon</option>
                    <option value="2">À Réparer</option>
                    <option value="3">Hors Service</option>
                  </select>
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

export default Assets;
