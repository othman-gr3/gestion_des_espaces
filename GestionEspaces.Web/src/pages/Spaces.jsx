import React, { useEffect, useState } from 'react';
import api from '../services/api';
import useAuth from '../hooks/useAuth';

const StatutConfig = {
  0: { label: 'Disponible',   tag: 'DISPO', cls: 'bg-success/10 text-success border-l-2 border-success' },
  1: { label: 'Maintenance',  tag: 'MAINT', cls: 'bg-warning/10 text-warning border-l-2 border-warning' },
  2: { label: 'Hors service', tag: 'HS',    cls: 'bg-danger/10 text-danger border-l-2 border-danger' },
};

const Spaces = () => {
  const { hasRole } = useAuth();
  const isGestionnaire = hasRole('Gestionnaire');

  const [bureaux, setBureaux] = useState([]);
  const [batiments, setBatiments] = useState([]);
  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [selectedBatimentId, setSelectedBatimentId] = useState('');
  const [statutFilter, setStatutFilter] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [totalCount, setTotalCount] = useState(0);

  const [isModalOpen, setIsModalOpen] = useState(false);
  const [currentBureau, setCurrentBureau] = useState(null);
  const [formData, setFormData] = useState({ numero: '', type: 'Bureau individuel', capacite: 1, superficie: 10, etage: 0, image: '', idBatiment: '', statut: 0 });
  const [formError, setFormError] = useState('');

  useEffect(() => { fetchSitesAndBatiments(); }, []);
  useEffect(() => { fetchBureaux(); }, [page, selectedBatimentId, statutFilter, searchText]);

  const fetchSitesAndBatiments = async () => {
    try {
      const [sitesRes, batimentsRes] = await Promise.all([api.get('/sites?pageSize=100'), api.get('/batiments?pageSize=100')]);
      setSites(sitesRes.data.items || []);
      setBatiments(batimentsRes.data.items || []);
    } catch (err) { console.error('Failed to load spaces data:', err); }
  };

  const fetchBureaux = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/bureaux', { params: { idBatiment: selectedBatimentId || undefined, searchText: searchText || undefined, statut: statutFilter !== '' ? parseInt(statutFilter) : undefined, pageNumber: page, pageSize } });
      setBureaux(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bureaux:', err);
      setError('Impossible de charger les espaces.');
    } finally { setLoading(false); }
  };

  const filteredBatiments = selectedSiteId ? batiments.filter((b) => b.idSite === parseInt(selectedSiteId)) : batiments;
  const handleSiteChange = (e) => { setSelectedSiteId(e.target.value); setSelectedBatimentId(''); setPage(1); };

  const handleOpenModal = (bureau = null) => {
    setFormError('');
    if (bureau) {
      setCurrentBureau(bureau);
      setFormData({ numero: bureau.numero, type: bureau.type || 'Bureau individuel', capacite: bureau.capacite, superficie: bureau.superficie, etage: bureau.etage, image: bureau.image || '', idBatiment: bureau.idBatiment, statut: bureau.statut });
    } else {
      setCurrentBureau(null);
      setFormData({ numero: '', type: 'Bureau individuel', capacite: 1, superficie: 10, etage: 0, image: '', idBatiment: selectedBatimentId || (filteredBatiments[0]?.idBatiment || ''), statut: 0 });
    }
    setIsModalOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');
    try {
      const payload = { ...formData, capacite: parseInt(formData.capacite), superficie: parseFloat(formData.superficie), etage: parseInt(formData.etage), idBatiment: parseInt(formData.idBatiment), statut: parseInt(formData.statut) };
      if (currentBureau) {
        await api.put(`/bureaux/${currentBureau.idBureau}`, { concurrencyToken: currentBureau.concurrencyToken, ...payload });
      } else {
        await api.post('/bureaux', payload);
      }
      setIsModalOpen(false);
      fetchBureaux();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  return (
    <div>
      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-5 py-3 mb-6">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 flex-1">
            <div>
              <label className="field-label">Site</label>
              <select value={selectedSiteId} onChange={handleSiteChange} className="form-field">
                <option value="">Tous les sites</option>
                {sites.map((s) => <option key={s.idSite} value={s.idSite}>{s.nom}</option>)}
              </select>
            </div>
            <div>
              <label className="field-label">Bâtiment</label>
              <select value={selectedBatimentId} onChange={(e) => { setSelectedBatimentId(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous</option>
                {filteredBatiments.map((b) => <option key={b.idBatiment} value={b.idBatiment}>{b.nom}</option>)}
              </select>
            </div>
            <div>
              <label className="field-label">Statut</label>
              <select value={statutFilter} onChange={(e) => { setStatutFilter(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous les statuts</option>
                <option value="0">Disponible</option>
                <option value="1">En maintenance</option>
                <option value="2">Hors service</option>
              </select>
            </div>
            <div>
              <label className="field-label">N° Bureau</label>
              <input type="text" placeholder="Ex: A-102" value={searchText} onChange={(e) => { setSearchText(e.target.value); setPage(1); }} className="form-field" />
            </div>
          </div>
          {isGestionnaire && (
            <button onClick={() => handleOpenModal()} className="sm:ml-4 self-end bg-primary px-5 py-2 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors flex-shrink-0" style={{ fontFamily: 'var(--font-mono)' }}>
              + Nouvel espace
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
              {['Numéro', "Type d'espace", 'Capacité', 'Superficie', 'Étage', 'Statut'].map((h) => (
                <th key={h} className="px-6 py-3 text-left"><span className="th-label">{h}</span></th>
              ))}
              {isGestionnaire && <th className="px-6 py-3 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isGestionnaire ? 7 : 6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Chargement des espaces...</td></tr>
            ) : bureaux.length === 0 ? (
              <tr><td colSpan={isGestionnaire ? 7 : 6} className="px-6 py-10 text-center text-[13px] text-text-secondary">Aucun espace ne correspond aux filtres.</td></tr>
            ) : (
              bureaux.map((b) => {
                const st = StatutConfig[b.statut] || { label: 'Inconnu', tag: '?', cls: '' };
                return (
                  <tr key={b.idBureau} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{b.numero}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[14px] text-text-primary">{b.type}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-primary">{b.capacite} poste{b.capacite > 1 ? 's' : ''}</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{b.superficie} m²</td>
                    <td className="whitespace-nowrap px-6 py-4 text-[13px] text-text-secondary">Étage {b.etage}</td>
                    <td className="whitespace-nowrap px-6 py-4"><span className={`status-tag ${st.cls}`}>{st.tag}</span></td>
                    {isGestionnaire && (
                      <td className="whitespace-nowrap px-6 py-4 text-right">
                        <button onClick={() => handleOpenModal(b)} className="btn-text-action btn-text-action-primary">Modifier</button>
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
                <div className="th-label mb-0.5">{currentBureau ? 'Modification' : 'Création'}</div>
                <h3 className="text-[17px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>{currentBureau ? "Modifier l'espace" : 'Nouvel espace'}</h3>
              </div>
              <button onClick={() => setIsModalOpen(false)} className="text-text-secondary hover:text-text-primary text-xl w-8 h-8 flex items-center justify-center">✕</button>
            </div>
            {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
            <form onSubmit={handleSubmit} className="p-6 space-y-5">
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Numéro</label>
                  <input type="text" value={formData.numero} onChange={(e) => setFormData((p) => ({ ...p, numero: e.target.value }))} className="form-field" required />
                </div>
                <div>
                  <label className="field-label">Type d'espace</label>
                  <input type="text" value={formData.type} onChange={(e) => setFormData((p) => ({ ...p, type: e.target.value }))} className="form-field" required />
                </div>
              </div>
              <div className="grid grid-cols-3 gap-5">
                <div>
                  <label className="field-label">Capacité</label>
                  <input type="number" value={formData.capacite} onChange={(e) => setFormData((p) => ({ ...p, capacite: e.target.value }))} className="form-field" min="1" required />
                </div>
                <div>
                  <label className="field-label">Superficie (m²)</label>
                  <input type="number" value={formData.superficie} onChange={(e) => setFormData((p) => ({ ...p, superficie: e.target.value }))} className="form-field" min="1" step="0.1" required />
                </div>
                <div>
                  <label className="field-label">Étage</label>
                  <input type="number" value={formData.etage} onChange={(e) => setFormData((p) => ({ ...p, etage: e.target.value }))} className="form-field" required />
                </div>
              </div>
              <div className="grid grid-cols-2 gap-5">
                <div>
                  <label className="field-label">Bâtiment parent</label>
                  <select value={formData.idBatiment} onChange={(e) => setFormData((p) => ({ ...p, idBatiment: e.target.value }))} className="form-field" required>
                    <option value="">Sélectionner...</option>
                    {batiments.map((bat) => <option key={bat.idBatiment} value={bat.idBatiment}>{bat.nom}</option>)}
                  </select>
                </div>
                <div>
                  <label className="field-label">Statut</label>
                  <select value={formData.statut} onChange={(e) => setFormData((p) => ({ ...p, statut: e.target.value }))} className="form-field" required>
                    <option value="0">Disponible</option>
                    <option value="1">En maintenance</option>
                    <option value="2">Hors service</option>
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

export default Spaces;
