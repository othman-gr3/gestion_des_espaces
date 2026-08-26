import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import StatusBadge from '../components/StatusBadge';
import Pagination from '../components/Pagination';
import SortableTh from '../components/SortableTh';
import EntityImage from '../components/EntityImage';
import ImageUploadField from '../components/ImageUploadField';
import useSort from '../hooks/useSort';

const EtatConfig = {
  0: { label: 'Neuf', tone: 'success' },
  1: { label: 'Bon état', tone: 'success' },
  2: { label: 'À réparer', tone: 'warning' },
  3: { label: 'Hors service', tone: 'danger' },
};

const getSortValue = (actif, col) => actif[col];

const Assets = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Administrateur');
  const isGestionnaire = hasRole('Gestionnaire');

  const [actifs, setActifs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [etatFilter, setEtatFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentActif, setCurrentActif] = useState(null);
  const [formData, setFormData] = useState({ nom: '', type: 'Ordinateur', marque: '', modele: '', numeroSerie: '', dateAchat: '', image: '', etat: 0 });
  const [formError, setFormError] = useState('');

  const { sortedRows, sortKey, sortDir, onSort } = useSort(actifs, getSortValue, 'nom');

  useEffect(() => { fetchActifs(); }, [page, searchText, etatFilter]);

  const fetchActifs = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/actifs', { params: { searchText: searchText || undefined, etat: etatFilter !== '' ? parseInt(etatFilter) : undefined, pageNumber: page, pageSize } });
      setActifs(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load assets:', err);
      setError('Impossible de charger la liste des matériels.');
    } finally { setLoading(false); }
  };

  const handleOpenDrawer = (actif = null) => {
    setFormError('');
    if (actif) {
      setCurrentActif(actif);
      setFormData({ nom: actif.nom, type: actif.type || '', marque: actif.marque || '', modele: actif.modele || '', numeroSerie: actif.numeroSerie || '', dateAchat: actif.dateAchat ? actif.dateAchat.split('T')[0] : '', image: actif.image || '', etat: actif.etat });
    } else {
      setCurrentActif(null);
      setFormData({ nom: '', type: 'Ordinateur', marque: '', modele: '', numeroSerie: '', dateAchat: '', image: '', etat: 0 });
    }
    setIsDrawerOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      const payload = { ...formData, dateAchat: formData.dateAchat ? new Date(formData.dateAchat).toISOString() : null, etat: parseInt(formData.etat) };
      if (currentActif) {
        await api.put(`/actifs/${currentActif.idActif}`, { concurrencyToken: currentActif.concurrencyToken, ...payload });
      } else {
        await api.post('/actifs', payload);
      }
      setIsDrawerOpen(false);
      fetchActifs();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  const handleDelete = async (actif) => {
    if (!window.confirm(`Supprimer le matériel "${actif.nom}" ?`)) return;
    try {
      await api.delete(`/actifs/${actif.idActif}`, { data: { concurrencyToken: actif.concurrencyToken } });
      fetchActifs();
    } catch (err) {
      console.error('Delete error:', err);
      alert(err.response?.data?.detail || 'Impossible de supprimer ce matériel.');
    }
  };

  const breadcrumbItems = isAdmin
    ? [{ label: 'Référentiel' }, { label: 'Actifs' }]
    : [{ label: 'Affectations' }, { label: 'Rechercher un actif' }];

  return (
    <div>
      <Breadcrumb items={breadcrumbItems} />

      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex gap-3 flex-1">
            <div className="flex-1 max-w-xs">
              <label className="field-label">Recherche</label>
              <input type="text" placeholder="Désignation ou n° de série..." value={searchText} onChange={(e) => { setSearchText(e.target.value); setPage(1); }} className="form-field" />
            </div>
            <div className="w-44">
              <label className="field-label">État</label>
              <select value={etatFilter} onChange={(e) => { setEtatFilter(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous les états</option>
                <option value="0">Neuf</option>
                <option value="1">Bon état</option>
                <option value="2">À réparer</option>
                <option value="3">Hors service</option>
              </select>
            </div>
          </div>
          {isAdmin && (
            <button onClick={() => handleOpenDrawer()} className="self-end bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
              + Nouvel actif
            </button>
          )}
        </div>
        {isGestionnaire && !isAdmin && (
          <div className="mt-2 text-[11.5px] text-text-secondary italic">Consultation seule — la gestion du référentiel actifs est réservée à l'Administrateur.</div>
        )}
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <SortableTh label="Désignation" column="nom" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Type" column="type" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Marque / Modèle</span></th>
              <SortableTh label="N° de série" column="numeroSerie" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">État</span></th>
              {isAdmin && <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isAdmin ? 6 : 5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des matériels...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={isAdmin ? 6 : 5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun matériel ne correspond aux critères.</td></tr>
            ) : (
              sortedRows.map((a) => {
                const et = EtatConfig[a.etat] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={a.idActif} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5">
                      <div className="flex items-center gap-3">
                        <EntityImage src={a.image} alt={a.nom} size={32} rounded="rounded" />
                        <span className="text-[13px] font-semibold text-text-primary">{a.nom}</span>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{a.type}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{[a.marque, a.modele].filter(Boolean).join(' · ') || '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{a.numeroSerie || '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={et.tone}>{et.label}</StatusBadge></td>
                    {isAdmin && (
                      <td className="whitespace-nowrap px-4 py-2.5 text-right">
                        <div className="flex items-center justify-end gap-4">
                          <button onClick={() => handleOpenDrawer(a)} className="btn-text-action btn-text-action-primary">Modifier</button>
                          <button onClick={() => handleDelete(a)} className="btn-text-action btn-text-action-danger">Supprimer</button>
                        </div>
                      </td>
                    )}
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} />

      {isAdmin && (
        <Drawer
          open={isDrawerOpen}
          onClose={() => setIsDrawerOpen(false)}
          eyebrow={currentActif ? 'Modification' : 'Création'}
          title={currentActif ? 'Modifier le matériel' : 'Nouvel actif'}
        >
          {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
          <form onSubmit={handleSubmit} className="p-6 space-y-5">
            <div className="grid grid-cols-2 gap-5">
              <div>
                <label className="field-label">Désignation</label>
                <input type="text" value={formData.nom} onChange={(e) => setFormData((p) => ({ ...p, nom: e.target.value }))} className="form-field" required />
              </div>
              <div>
                <label className="field-label">Type</label>
                <input type="text" value={formData.type} onChange={(e) => setFormData((p) => ({ ...p, type: e.target.value }))} className="form-field" />
              </div>
            </div>
            <div className="grid grid-cols-3 gap-5">
              <div>
                <label className="field-label">Marque</label>
                <input type="text" value={formData.marque} onChange={(e) => setFormData((p) => ({ ...p, marque: e.target.value }))} className="form-field" />
              </div>
              <div>
                <label className="field-label">Modèle</label>
                <input type="text" value={formData.modele} onChange={(e) => setFormData((p) => ({ ...p, modele: e.target.value }))} className="form-field" />
              </div>
              <div>
                <label className="field-label">N° de série</label>
                <input type="text" value={formData.numeroSerie} onChange={(e) => setFormData((p) => ({ ...p, numeroSerie: e.target.value }))} className="form-field" />
              </div>
            </div>
            <div className="grid grid-cols-2 gap-5">
              <div>
                <label className="field-label">Date d'achat</label>
                <input type="date" value={formData.dateAchat} onChange={(e) => setFormData((p) => ({ ...p, dateAchat: e.target.value }))} className="form-field" />
              </div>
              <div>
                <label className="field-label">État</label>
                <select value={formData.etat} onChange={(e) => setFormData((p) => ({ ...p, etat: e.target.value }))} className="form-field" required>
                  <option value="0">Neuf</option>
                  <option value="1">Bon état</option>
                  <option value="2">À réparer</option>
                  <option value="3">Hors service</option>
                </select>
              </div>
            </div>
            <ImageUploadField
              value={formData.image}
              onChange={(url) => setFormData((p) => ({ ...p, image: url }))}
              alt={formData.nom}
            />
            <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
              <button type="button" onClick={() => setIsDrawerOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
              <button type="submit" className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>Enregistrer</button>
            </div>
          </form>
        </Drawer>
      )}
    </div>
  );
};

export default Assets;
