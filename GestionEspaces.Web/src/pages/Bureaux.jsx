import React, { useEffect, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import api from '../services/api';
import useAuth from '../hooks/useAuth';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import StatusBadge from '../components/StatusBadge';
import Pagination from '../components/Pagination';
import SortableTh from '../components/SortableTh';
import EntityImage from '../components/EntityImage';
import useSort from '../hooks/useSort';

const StatutConfig = {
  0: { label: 'Disponible', tone: 'success' },
  1: { label: 'Occupé', tone: 'warning' },
  2: { label: 'En maintenance', tone: 'danger' },
};

const TypeConfig = {
  0: 'Individuel',
  1: 'Open space',
  2: 'Salle de réunion',
};

const getSortValue = (b, col) => b[col];

const Bureaux = () => {
  const { hasRole } = useAuth();
  const isAdmin = hasRole('Administrateur');
  const [searchParams, setSearchParams] = useSearchParams();

  const [bureaux, setBureaux] = useState([]);
  const [batiments, setBatiments] = useState([]);
  const [sites, setSites] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [selectedSiteId, setSelectedSiteId] = useState('');
  const [selectedBatimentId, setSelectedBatimentId] = useState(searchParams.get('idBatiment') || '');
  const [statutFilter, setStatutFilter] = useState('');
  const [searchText, setSearchText] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(15);
  const [totalCount, setTotalCount] = useState(0);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [currentBureau, setCurrentBureau] = useState(null);
  const [formData, setFormData] = useState({ numero: '', type: 0, capacite: 1, superficie: 10, etage: 0, image: '', idBatiment: '', statut: 0 });
  const [formError, setFormError] = useState('');

  const { sortedRows, sortKey, sortDir, onSort } = useSort(bureaux, getSortValue, 'numero');

  useEffect(() => { fetchSitesAndBatiments(); }, []);
  useEffect(() => { fetchBureaux(); }, [page, selectedBatimentId, statutFilter, searchText]);

  const fetchSitesAndBatiments = async () => {
    try {
      const [sitesRes, batimentsRes] = await Promise.all([api.get('/sites?pageSize=100'), api.get('/batiments?pageSize=100')]);
      setSites(sitesRes.data.items || []);
      setBatiments(batimentsRes.data.items || []);
    } catch (err) { console.error('Failed to load referentiel data:', err); }
  };

  const fetchBureaux = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/bureaux', {
        params: { idBatiment: selectedBatimentId || undefined, searchText: searchText || undefined, statut: statutFilter !== '' ? parseInt(statutFilter) : undefined, pageNumber: page, pageSize },
      });
      setBureaux(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load bureaux:', err);
      setError('Impossible de charger les bureaux.');
    } finally { setLoading(false); }
  };

  const filteredBatiments = selectedSiteId ? batiments.filter((b) => b.idSite === parseInt(selectedSiteId)) : batiments;
  const activeBatiment = batiments.find((b) => b.idBatiment === parseInt(selectedBatimentId));

  const handleSiteChange = (e) => { setSelectedSiteId(e.target.value); setSelectedBatimentId(''); setPage(1); setSearchParams({}); };
  const handleBatimentChange = (e) => {
    setSelectedBatimentId(e.target.value);
    setPage(1);
    setSearchParams(e.target.value ? { idBatiment: e.target.value } : {});
  };

  const handleOpenDrawer = (bureau = null) => {
    setFormError('');
    if (bureau) {
      setCurrentBureau(bureau);
      setFormData({ numero: bureau.numero, type: bureau.type ?? 0, capacite: bureau.capacite, superficie: bureau.superficie, etage: bureau.etage, image: bureau.image || '', idBatiment: bureau.idBatiment, statut: bureau.statut });
    } else {
      setCurrentBureau(null);
      setFormData({ numero: '', type: 0, capacite: 1, superficie: 10, etage: 0, image: '', idBatiment: selectedBatimentId || (filteredBatiments[0]?.idBatiment || ''), statut: 0 });
    }
    setIsDrawerOpen(true);
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
      setIsDrawerOpen(false);
      fetchBureaux();
    } catch (err) {
      console.error('Submit error:', err);
      setFormError(err.response?.data?.detail || "Une erreur est survenue lors de l'enregistrement.");
    }
  };

  const handleDelete = async (bureau) => {
    if (!window.confirm(`Supprimer le bureau "${bureau.numero}" ?`)) return;
    try {
      await api.delete(`/bureaux/${bureau.idBureau}`, { data: { concurrencyToken: bureau.concurrencyToken } });
      fetchBureaux();
    } catch (err) {
      console.error('Delete error:', err);
      alert(err.response?.data?.detail || 'Impossible de supprimer ce bureau.');
    }
  };

  const breadcrumbItems = activeBatiment
    ? [{ label: 'Référentiel' }, { label: 'Bâtiments', href: '/batiments' }, { label: activeBatiment.nom, href: `/batiments` }, { label: 'Bureaux' }]
    : [{ label: 'Référentiel' }, { label: 'Bureaux' }];

  return (
    <div>
      <Breadcrumb items={breadcrumbItems} />

      {/* Toolbar */}
      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
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
              <select value={selectedBatimentId} onChange={handleBatimentChange} className="form-field">
                <option value="">Tous</option>
                {filteredBatiments.map((b) => <option key={b.idBatiment} value={b.idBatiment}>{b.nom}</option>)}
              </select>
            </div>
            <div>
              <label className="field-label">Statut</label>
              <select value={statutFilter} onChange={(e) => { setStatutFilter(e.target.value); setPage(1); }} className="form-field">
                <option value="">Tous les statuts</option>
                <option value="0">Disponible</option>
                <option value="1">Occupé</option>
                <option value="2">En maintenance</option>
              </select>
            </div>
            <div>
              <label className="field-label">N° Bureau</label>
              <input type="text" placeholder="Ex: A-102" value={searchText} onChange={(e) => { setSearchText(e.target.value); setPage(1); }} className="form-field" />
            </div>
          </div>
          {isAdmin && (
            <button onClick={() => handleOpenDrawer()} className="sm:ml-4 self-end bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors flex-shrink-0" style={{ fontFamily: 'var(--font-mono)' }}>
              + Nouveau bureau
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
              <SortableTh label="Numéro" column="numero" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Type" column="type" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Capacité" column="capacite" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Superficie" column="superficie" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Étage" column="etage" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
              {isAdmin && <th className="px-4 py-2.5 text-right"><span className="th-label">Actions</span></th>}
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={isAdmin ? 7 : 6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des bureaux...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={isAdmin ? 7 : 6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun bureau ne correspond aux filtres.</td></tr>
            ) : (
              sortedRows.map((b) => {
                const st = StatutConfig[b.statut] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={b.idBureau} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5">
                      <div className="flex items-center gap-3">
                        <EntityImage src={b.image} alt={b.numero} size={32} rounded="rounded" />
                        <span className="text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{b.numero}</span>
                      </div>
                    </td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] text-text-primary">{TypeConfig[b.type] ?? '—'}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{b.capacite} poste{b.capacite > 1 ? 's' : ''}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>{b.superficie} m²</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">Étage {b.etage}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={st.tone}>{st.label}</StatusBadge></td>
                    {isAdmin && (
                      <td className="whitespace-nowrap px-4 py-2.5 text-right">
                        <button onClick={() => handleOpenDrawer(b)} className="btn-text-action btn-text-action-primary">Modifier</button>
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

      <Drawer
        open={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        eyebrow={currentBureau ? 'Modification' : 'Création'}
        title={currentBureau ? 'Modifier le bureau' : 'Nouveau bureau'}
      >
        {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Numéro</label>
              <input type="text" value={formData.numero} onChange={(e) => setFormData((p) => ({ ...p, numero: e.target.value }))} className="form-field" required />
            </div>
            <div>
              <label className="field-label">Type d'espace</label>
              <select value={formData.type} onChange={(e) => setFormData((p) => ({ ...p, type: e.target.value }))} className="form-field" required>
                <option value="0">Individuel</option>
                <option value="1">Open space</option>
                <option value="2">Salle de réunion</option>
              </select>
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
              {parseInt(formData.statut) === 1 ? (
                <select value={formData.statut} className="form-field" disabled>
                  <option value="1">Occupé (géré automatiquement)</option>
                </select>
              ) : (
                <select value={formData.statut} onChange={(e) => setFormData((p) => ({ ...p, statut: e.target.value }))} className="form-field" required>
                  <option value="0">Disponible</option>
                  <option value="2">En maintenance</option>
                </select>
              )}
            </div>
          </div>
          <div>
            <label className="field-label">Photo (URL, optionnel)</label>
            <input type="text" value={formData.image} onChange={(e) => setFormData((p) => ({ ...p, image: e.target.value }))} className="form-field" placeholder="https://..." />
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

export default Bureaux;
