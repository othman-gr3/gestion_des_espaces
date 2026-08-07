import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const EtatConfig = {
  0: { label: 'Neuf',        tag: 'NEUF', cls: 'bg-success/10 text-success border-l-2 border-success' },
  1: { label: 'Bon état',    tag: 'BON',  cls: 'bg-success/10 text-success border-l-2 border-success' },
  2: { label: 'À réparer',   tag: 'REP',  cls: 'bg-warning/10 text-warning border-l-2 border-warning' },
  3: { label: 'Hors service', tag: 'HS',  cls: 'bg-danger/10 text-danger border-l-2 border-danger' },
};

const Assets = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [actifs, setActifs] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchText, setSearchText] = useState('');
  const [etatFilter, setEtatFilter] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentActif, setCurrentActif] = useState(null);
  const [formData, setFormData] = useState({ nom: '', type: 'Ordinateur', marque: '', modele: '', numeroSerie: '', dateAchat: '', image: '', etat: 0 });
  const [formError, setFormError] = useState('');

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

  const handleOpenModal = (actif = null) => {
    setFormError('');
    if (actif) {
      setCurrentActif(actif);
      setFormData({ nom: actif.nom, type: actif.type || '', marque: actif.marque || '', modele: actif.modele || '', numeroSerie: actif.numeroSerie || '', dateAchat: actif.dateAchat ? actif.dateAchat.split('T')[0] : '', image: actif.image || '', etat: actif.etat });
    } else {
      setCurrentActif(null);
      setFormData({ nom: '', type: 'Ordinateur', marque: '', modele: '', numeroSerie: '', dateAchat: '', image: '', etat: 0 });
    }
    setIsModalOpen(true);
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
      setIsModalOpen(false);
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

  return (
    <div>
      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-5 py-3 mb-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="flex gap-3 flex-1">
            <div className="flex-1 max-w-xs">
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
          {isGestionnaire && (
            <button onClick={() => handleOpenModal()} className="self-end bg-primary px-5 py-2 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
              + Nouvel actif
            </button>
          )}
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {/* Table */}
      <div className="border border-border-subtle bg-surface-bg overflow-hidden">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              {['Désignation', 'Type', 'Marque / Modèle', 'N° de série', 'État'].map((h) => (
                <th key={h} className="px-6 py-3 text-left"><span className="th-label">{h}</span></th>
              ))}
              {isGestionnaire && <th className="px-6 py-3 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isGestionnaire ? 6 : 5} className="px-6 py-10 text-center text-[13px] text-text-secondary">Chargement des matériels...</td></tr>
            ) : actifs.length === 0 ? (
              <tr><td colSpan={isGestionnaire ? 6 : 5} className="px-6 py-10 text-center text-[13px] text-text-secondary">Aucun matériel ne correspond aux critères.</td></tr>
            ) : (
              actifs.map((a) => {
                const et = EtatConfig[a.etat] || { label: 'Inconnu', tag: '?', cls: '' };
                return (
                  <tr key={a.idActif} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-6 py-4 text-[14px] font-semibold text-text-primary">{a.nom}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-secondary">{a.type}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-secondary">{[a.marque, a.modele].filter(Boolean).join(' · ') || '—'}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{a.numeroSerie || '—'}</td>
                    <td className="whitespace-nowrap px-6 py-4"><span className={`status-tag ${et.cls}`}>{et.tag}</span></td>
                    {isGestionnaire && (
                      <td className="whitespace-nowrap px-6 py-4 text-right">
                        <div className="flex items-center justify-end gap-5">
                          <button onClick={() => handleOpenModal(a)} className="btn-text-action btn-text-action-primary">Modifier</button>
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

      {/* Pagination */}
      {totalCount > pageSize && (
        <div className="flex items-center justify-between border-t border-border-subtle pt-4 mt-4">
          <button disabled={page === 1} onClick={() => setPage((p) => Math.max(p - 1, 1))} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40">← Précédent</button>
          <span className="text-[12px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>Page {page} / {Math.ceil(totalCount / pageSize)}</span>
          <button disabled={page * pageSize >= totalCount} onClick={() => setPage((p) => p + 1)} className="text-[13px] font-medium text-primary hover:text-primary-dark disabled:opacity-40">Suivant →</button>
        </div>
      )}

      {/* Modal */}
      {isModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={() => setIsModalOpen(false)}>
          <div className="modal-slide-in h-full w-full max-w-lg bg-surface-bg border-l-2 border-primary overflow-y-auto" onClick={(e) => e.stopPropagation()}>
            <div className="flex items-center justify-between border-b-2 border-primary px-6 py-4 bg-neutral-bg">
              <div>
                <div className="th-label mb-0.5">{currentActif ? 'Modification' : 'Création'}</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>{currentActif ? 'Modifier le matériel' : 'Nouvel actif'}</h3>
              </div>
              <button onClick={() => setIsModalOpen(false)} className="text-text-secondary hover:text-text-primary text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>
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

export default Assets;
