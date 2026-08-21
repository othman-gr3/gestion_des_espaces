import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import Pagination from '../components/Pagination';

const EVENT_LABELS = {
  AgentAffecteAuBureauEvent: 'Affectation de poste créée',
  AffectationPosteClotureeEvent: 'Affectation de poste clôturée',
  BureauMisEnMaintenanceEvent: 'Bureau mis en maintenance',
  BureauRemisEnServiceEvent: 'Bureau remis en service',
  ActifAffecteEvent: "Affectation d'actif créée",
  AffectationActifClotureeEvent: "Affectation d'actif clôturée",
};

const formatPayload = (payload) => {
  try {
    const parsed = JSON.parse(payload);
    return Object.entries(parsed)
      .map(([key, value]) => `${key}: ${value}`)
      .join(' · ');
  } catch {
    return payload;
  }
};

const JournalAudit = () => {
  const [entries, setEntries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [totalCount, setTotalCount] = useState(0);

  useEffect(() => { fetchEntries(); }, [page]);

  const fetchEntries = async () => {
    setLoading(true);
    setError('');
    try {
      const response = await api.get('/audit-log', { params: { pageNumber: page, pageSize } });
      setEntries(response.data.items || []);
      setTotalCount(response.data.totalCount || 0);
    } catch (err) {
      console.error('Failed to load audit log:', err);
      setError("Impossible de charger le journal d'audit.");
    } finally { setLoading(false); }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Sécurité' }, { label: "Journal d'audit" }]} />

      <div className="mb-4 text-[12.5px] text-text-secondary">
        Historique des événements métier significatifs (affectations, changements de statut de bureau) — qui a fait quoi, et quand.
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Date / heure</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Événement</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Utilisateur</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Rôle</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Détails</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement du journal...</td></tr>
            ) : entries.length === 0 ? (
              <tr><td colSpan={5} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun événement enregistré.</td></tr>
            ) : (
              entries.map((entry) => (
                <tr key={entry.idAuditLog} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>
                    {new Date(entry.occurredOnUtc).toLocaleString('fr-FR')}
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[13px] font-medium text-text-primary">
                    {EVENT_LABELS[entry.eventType] || entry.eventType}
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">
                    {entry.utilisateurEmail || <span className="italic text-text-secondary">Système</span>}
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">
                    {entry.utilisateurRole || '—'}
                  </td>
                  <td className="px-4 py-2.5 text-[11.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }} title={entry.payload}>
                    {formatPayload(entry.payload)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={pageSize} totalCount={totalCount} onPageChange={setPage} />
    </div>
  );
};

export default JournalAudit;
