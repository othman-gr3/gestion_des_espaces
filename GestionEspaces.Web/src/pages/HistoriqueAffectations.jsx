import React, { useEffect, useState } from 'react';
import { listBureaux, listActifs, fetchAllAssignments } from '../services/affectationService';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';
import SortableTh from '../components/SortableTh';
import useSort from '../hooks/useSort';

const getSortValue = (row, col) => {
  if (col === 'agent') return `${row.agent.nom} ${row.agent.prenom}`;
  if (col === 'ressource') return row.ressourceLabel;
  if (col === 'dateAffectation' || col === 'dateFin') return row[col] ? new Date(row[col]).getTime() : 0;
  return row[col];
};

const HistoriqueAffectations = () => {
  const [agents, setAgents] = useState([]);
  const [bureaux, setBureaux] = useState([]);
  const [actifs, setActifs] = useState([]);
  const [rows, setRows] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const [typeFilter, setTypeFilter] = useState('');
  const [agentFilter, setAgentFilter] = useState('');
  const [bureauFilter, setBureauFilter] = useState('');
  const [actifFilter, setActifFilter] = useState('');
  const [statutFilter, setStatutFilter] = useState('');

  useEffect(() => { load(); }, []);

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      const [{ agents: agentsList, posteAffectations, actifAffectations }, bureauxRes, actifsRes] = await Promise.all([
        fetchAllAssignments(),
        listBureaux(),
        listActifs(),
      ]);
      setAgents(agentsList);
      setBureaux(bureauxRes.data.items || []);
      setActifs(actifsRes.data.items || []);

      const bureauMap = new Map((bureauxRes.data.items || []).map((b) => [b.idBureau, b]));
      const actifMap = new Map((actifsRes.data.items || []).map((a) => [a.idActif, a]));

      const posteRows = posteAffectations.map((a) => ({
        id: `poste-${a.idAffectationPoste}`,
        type: 'poste',
        agent: a.agent,
        agentId: a.agentId,
        resourceId: a.bureauId,
        ressourceLabel: bureauMap.has(a.bureauId) ? `N° ${bureauMap.get(a.bureauId).numero}` : `Bureau ${a.bureauId}`,
        dateAffectation: a.dateAffectation,
        dateFin: a.dateFin,
      }));

      const actifRows = actifAffectations.map((a) => ({
        id: `actif-${a.idAffectationActif}`,
        type: 'actif',
        agent: a.agent,
        agentId: a.agentId,
        resourceId: a.actifId,
        ressourceLabel: actifMap.has(a.actifId) ? actifMap.get(a.actifId).nom : `Actif ${a.actifId}`,
        dateAffectation: a.dateAffectation,
        dateFin: a.dateFin,
      }));

      setRows([...posteRows, ...actifRows]);
    } catch (err) {
      console.error('Failed to load history:', err);
      setError("Impossible de charger l'historique des affectations.");
    } finally { setLoading(false); }
  };

  const filtered = rows.filter((r) => {
    if (typeFilter && r.type !== typeFilter) return false;
    if (agentFilter && String(r.agentId) !== agentFilter) return false;
    if (bureauFilter && !(r.type === 'poste' && String(r.resourceId) === bureauFilter)) return false;
    if (actifFilter && !(r.type === 'actif' && String(r.resourceId) === actifFilter)) return false;
    if (statutFilter === 'active' && r.dateFin) return false;
    if (statutFilter === 'terminee' && !r.dateFin) return false;
    return true;
  });

  const { sortedRows, sortKey, sortDir, onSort } = useSort(filtered, getSortValue, 'dateAffectation', 'desc');

  return (
    <div>
      <Breadcrumb items={[{ label: 'Affectations' }, { label: 'Historique' }]} />

      <div className="border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-5">
          <div>
            <label className="field-label">Type</label>
            <select value={typeFilter} onChange={(e) => setTypeFilter(e.target.value)} className="form-field">
              <option value="">Postes et actifs</option>
              <option value="poste">Postes</option>
              <option value="actif">Actifs</option>
            </select>
          </div>
          <div>
            <label className="field-label">Agent</label>
            <select value={agentFilter} onChange={(e) => setAgentFilter(e.target.value)} className="form-field">
              <option value="">Tous les agents</option>
              {agents.map((a) => <option key={a.idAgent} value={a.idAgent}>{a.nom.toUpperCase()} {a.prenom}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Bureau</label>
            <select value={bureauFilter} onChange={(e) => setBureauFilter(e.target.value)} className="form-field" disabled={typeFilter === 'actif'}>
              <option value="">Tous les bureaux</option>
              {bureaux.map((b) => <option key={b.idBureau} value={b.idBureau}>N° {b.numero}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Actif</label>
            <select value={actifFilter} onChange={(e) => setActifFilter(e.target.value)} className="form-field" disabled={typeFilter === 'poste'}>
              <option value="">Tous les actifs</option>
              {actifs.map((a) => <option key={a.idActif} value={a.idActif}>{a.nom}</option>)}
            </select>
          </div>
          <div>
            <label className="field-label">Statut</label>
            <select value={statutFilter} onChange={(e) => setStatutFilter(e.target.value)} className="form-field">
              <option value="">Toutes</option>
              <option value="active">Active</option>
              <option value="terminee">Terminée</option>
            </select>
          </div>
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Type</span></th>
              <SortableTh label="Agent" column="agent" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Ressource" column="ressource" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Début" column="dateAffectation" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <SortableTh label="Fin" column="dateFin" sortKey={sortKey} sortDir={sortDir} onSort={onSort} />
              <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement de l'historique...</td></tr>
            ) : sortedRows.length === 0 ? (
              <tr><td colSpan={6} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucune affectation ne correspond aux filtres.</td></tr>
            ) : (
              sortedRows.map((r) => (
                <tr key={r.id} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <span className="status-tag bg-primary/10 text-primary border-l-2 border-primary">{r.type === 'poste' ? 'POSTE' : 'ACTIF'}</span>
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">{r.agent.nom.toUpperCase()} {r.agent.prenom}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{r.ressourceLabel}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{new Date(r.dateAffectation).toLocaleDateString('fr-FR')}</td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{r.dateFin ? new Date(r.dateFin).toLocaleDateString('fr-FR') : '—'}</td>
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <StatusBadge tone={r.dateFin ? 'neutral' : 'success'}>{r.dateFin ? 'Terminée' : 'Active'}</StatusBadge>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default HistoriqueAffectations;
