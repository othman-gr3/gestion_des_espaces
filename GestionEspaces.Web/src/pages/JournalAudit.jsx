import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import Pagination from '../components/Pagination';
import StatusBadge from '../components/StatusBadge';

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

  const [anomalies, setAnomalies] = useState(null);
  const [anomaliesUsedAi, setAnomaliesUsedAi] = useState(false);
  const [anomaliesEntriesAnalyzed, setAnomaliesEntriesAnalyzed] = useState(0);
  const [analyzing, setAnalyzing] = useState(false);
  const [anomaliesError, setAnomaliesError] = useState('');

  useEffect(() => { fetchEntries(); }, [page]);

  const analyzeAnomalies = async () => {
    setAnalyzing(true);
    setAnomaliesError('');
    try {
      const response = await api.post('/audit-log/anomalies');
      setAnomalies(response.data.findings || []);
      setAnomaliesUsedAi(!!response.data.usedAi);
      setAnomaliesEntriesAnalyzed(response.data.entriesAnalyzed || 0);
    } catch (err) {
      console.error('Anomaly detection error:', err);
      setAnomaliesError("L'analyse des anomalies a échoué.");
    } finally { setAnalyzing(false); }
  };

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

      <div className="mb-4 flex items-start justify-between gap-4">
        <div className="text-[12.5px] text-text-secondary">
          Historique des événements métier significatifs (affectations, changements de statut de bureau) — qui a fait quoi, et quand.
        </div>
        <button
          type="button"
          onClick={analyzeAnomalies}
          disabled={analyzing}
          className="bg-primary px-4 py-2 text-[11px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50 whitespace-nowrap"
          style={{ fontFamily: 'var(--font-mono)' }}
        >
          {analyzing ? 'Analyse...' : 'Analyser les anomalies'}
        </button>
      </div>

      {anomaliesError && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{anomaliesError}</div>}

      {anomalies !== null && (
        <div className="mb-6 border border-border-subtle bg-surface-bg p-4">
          <div className="mb-3 flex items-center justify-between gap-4">
            <div className="th-label">Résultat de l'analyse ({anomaliesEntriesAnalyzed} événement{anomaliesEntriesAnalyzed > 1 ? 's' : ''} examiné{anomaliesEntriesAnalyzed > 1 ? 's' : ''})</div>
            <StatusBadge tone={anomaliesUsedAi ? 'success' : 'warning'}>{anomaliesUsedAi ? 'IA activée' : 'Heuristique locale'}</StatusBadge>
          </div>
          {anomalies.length === 0 ? (
            <div className="text-[12.5px] text-text-secondary italic">Aucun événement à analyser.</div>
          ) : (
            <div className="flex flex-col gap-2">
              {anomalies.map((finding, i) => (
                <div
                  key={i}
                  className={`px-3.5 py-2.5 text-[13px] border-l-[3px] ${
                    finding.severity === 'warning' ? 'border-warning bg-warning/5' : 'border-accent bg-accent/5'
                  }`}
                >
                  <div className="font-semibold text-text-primary">{finding.title}</div>
                  <div className="mt-0.5 text-[12.5px] text-text-secondary">{finding.description}</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

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
