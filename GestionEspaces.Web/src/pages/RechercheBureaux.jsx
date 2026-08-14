import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { listBatiments, listBureaux } from '../services/affectationService';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';
import SortableTh from '../components/SortableTh';
import useSort from '../hooks/useSort';

const StatutConfig = {
  0: { label: 'Disponible', tone: 'success' },
  1: { label: 'Maintenance', tone: 'warning' },
  2: { label: 'Hors service', tone: 'danger' },
};

const getSortValue = (b, col) => b[col];

const RechercheBureaux = () => {
  const navigate = useNavigate();

  const [batiments, setBatiments] = useState([]);
  const [bureaux, setBureaux] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [selectedBatimentId, setSelectedBatimentId] = useState('');
  const [capaciteMin, setCapaciteMin] = useState('');
  const [statutFilter, setStatutFilter] = useState('0');

  const results = bureaux.filter((b) => !capaciteMin || b.capacite >= parseInt(capaciteMin, 10));
  const { sortedRows, sortKey, sortDir, onSort } = useSort(results, getSortValue, 'numero');

  useEffect(() => {
    listBatiments().then((res) => setBatiments(res.data.items || [])).catch((err) => console.error('Failed to load batiments:', err));
  }, []);

  useEffect(() => { fetchBureaux(); }, [selectedBatimentId, statutFilter]);

  const fetchBureaux = async () => {
    setLoading(true);
    setError('');
    try {
      const res = await listBureaux({
        idBatiment: selectedBatimentId || undefined,
        statut: statutFilter !== '' ? statutFilter : undefined,
      });
      setBureaux(res.data.items || []);
    } catch (err) {
      console.error('Failed to search bureaux:', err);
      setError('Impossible de rechercher les bureaux.');
    } finally { setLoading(false); }
  };

  const batimentName = (idBatiment) => batiments.find((b) => b.idBatiment === idBatiment)?.nom || `Bâtiment ${idBatiment}`;

  return (
    <div>
      <Breadcrumb items={[{ label: 'Affectations' }, { label: 'Rechercher un bureau' }]} />

      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
          <div>
            <label className="field-label">Bâtiment</label>
            <select value={selectedBatimentId} onChange={(e) => setSelectedBatimentId(e.target.value)} className="form-field">
              <option value="">Tous les bâtiments</option>
              {batiments.map((b) => <option key={b.idBatiment} value={b.idBatiment}>{b.nom}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Capacité minimale</label>
            <input type="number" min="0" placeholder="Ex: 4" value={capaciteMin} onChange={(e) => setCapaciteMin(e.target.value)} className="form-field" />
          </div>
          <div>
            <label className="field-label">Statut</label>
            <select value={statutFilter} onChange={(e) => setStatutFilter(e.target.value)} className="form-field">
              <option value="">Tous les statuts</option>
              <option value="0">Disponible</option>
              <option value="1">En maintenance</option>
              <option value="2">Hors service</option>
            </select>
          </div>
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <SortableTh label="Numéro" column="numero" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Bâtiment</span></th>
              <SortableTh label="Type" column="type" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Capacité" column="capacite" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
              <th className="px-4 py-2.5 text-right"><span className="th-label">Action</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Recherche en cours...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun bureau ne correspond aux critères.</td></tr>
            ) : (
              sortedRows.map((b) => {
                const st = StatutConfig[b.statut] || { label: 'Inconnu', tone: 'neutral' };
                return (
                  <tr key={b.idBureau} className="hover:bg-neutral-bg/60 transition-colors">
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{b.numero}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{batimentName(b.idBatiment)}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[13px] text-text-primary">{b.type}</td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{b.capacite} poste{b.capacite > 1 ? 's' : ''}</td>
                    <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={st.tone}>{st.label}</StatusBadge></td>
                    <td className="whitespace-nowrap px-4 py-2.5 text-right">
                      <button
                        onClick={() => navigate('/affectations-poste', { state: { bureauId: b.idBureau, bureauNumero: b.numero } })}
                        className="btn-text-action btn-text-action-primary"
                        disabled={b.statut !== 0}
                        title={b.statut !== 0 ? "Ce bureau n'est pas disponible" : undefined}
                      >
                        Affecter cet agent →
                      </button>
                    </td>
                  </tr>
                );
              })
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default RechercheBureaux;
