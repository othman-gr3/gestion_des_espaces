import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import api from '../services/api';
import { listBatiments } from '../services/affectationService';
import Breadcrumb from '../components/Breadcrumb';
import StatusBadge from '../components/StatusBadge';

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

const EXAMPLES = [
  'Un bureau individuel disponible avec au moins 2 places',
  'Une salle de réunion pour 8 personnes au bâtiment Direction Générale',
  'Un open space disponible à partir du 2ème étage',
];

const RechercheIA = () => {
  const navigate = useNavigate();

  const [batiments, setBatiments] = useState([]);
  const [query, setQuery] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [results, setResults] = useState(null);
  const [summary, setSummary] = useState('');
  const [usedAi, setUsedAi] = useState(false);

  useEffect(() => {
    listBatiments().then((res) => setBatiments(res.data.items || [])).catch((err) => console.error('Failed to load batiments:', err));
  }, []);

  const batimentName = (idBatiment) => batiments.find((b) => b.idBatiment === idBatiment)?.nom || `Bâtiment ${idBatiment}`;

  const handleSearch = async (e) => {
    e.preventDefault();
    if (!query.trim()) return;
    setLoading(true);
    setError('');
    try {
      const response = await api.post('/bureaux/ai-search', { query });
      setResults(response.data.results || []);
      setSummary(response.data.summary || '');
      setUsedAi(!!response.data.usedAi);
    } catch (err) {
      console.error('AI search error:', err);
      setError(err.response?.data?.detail || "La recherche a échoué.");
      setResults(null);
    } finally { setLoading(false); }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Intelligence artificielle' }, { label: 'Recherche IA' }]} />

      <div className="struct-card p-5 mb-6">
        <div className="th-label mb-3">Décrivez le bureau que vous cherchez</div>
        <form onSubmit={handleSearch} className="flex flex-col gap-3 sm:flex-row sm:items-start">
          <textarea
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            placeholder="Ex : un bureau individuel disponible avec au moins 3 places au Siège ONEE"
            className="form-field flex-1 min-h-[52px] resize-none"
            rows={2}
          />
          <button
            type="submit"
            disabled={loading || !query.trim()}
            className="bg-primary px-5 py-2.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50 whitespace-nowrap"
            style={{ fontFamily: 'var(--font-mono)' }}
          >
            {loading ? 'Recherche...' : 'Rechercher'}
          </button>
        </form>
        <div className="mt-3 flex flex-wrap gap-2">
          {EXAMPLES.map((ex) => (
            <button
              key={ex}
              type="button"
              onClick={() => setQuery(ex)}
              className="text-[11px] text-text-secondary hover:text-primary border border-border-subtle px-2.5 py-1 transition-colors"
            >
              {ex}
            </button>
          ))}
        </div>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      {results !== null && (
        <>
          <div className="mb-4 border-l-[3px] border-accent bg-accent/5 px-4 py-3 flex items-start justify-between gap-4">
            <div className="text-[13px] text-text-primary">{summary}</div>
            <StatusBadge tone={usedAi ? 'success' : 'warning'}>{usedAi ? 'IA activée' : 'Repli mot-clé'}</StatusBadge>
          </div>

          <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
            <table className="min-w-full">
              <thead>
                <tr className="border-b-2 border-primary bg-neutral-bg">
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Numéro</span></th>
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Bâtiment</span></th>
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Type</span></th>
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Capacité</span></th>
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Étage</span></th>
                  <th className="px-4 py-2.5 text-left"><span className="th-label">Statut</span></th>
                  <th className="px-4 py-2.5 text-right"><span className="th-label">Action</span></th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border-subtle">
                {results.length === 0 ? (
                  <tr><td colSpan={7} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun bureau ne correspond à cette demande.</td></tr>
                ) : (
                  results.map((b) => {
                    const st = StatutConfig[b.statut] || { label: 'Inconnu', tone: 'neutral' };
                    return (
                      <tr key={b.idBureau} className="hover:bg-neutral-bg/60 transition-colors">
                        <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] font-semibold text-primary" style={{ fontFamily: 'var(--font-mono)' }}>{b.numero}</td>
                        <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{batimentName(b.idBatiment)}</td>
                        <td className="whitespace-nowrap px-4 py-2.5 text-[13px] text-text-primary">{TypeConfig[b.type] ?? '—'}</td>
                        <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-primary">{b.capacite} poste{b.capacite > 1 ? 's' : ''}</td>
                        <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">Étage {b.etage}</td>
                        <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={st.tone}>{st.label}</StatusBadge></td>
                        <td className="whitespace-nowrap px-4 py-2.5 text-right">
                          <button
                            onClick={() => navigate('/affectations-poste', { state: { bureauId: b.idBureau, bureauNumero: b.numero } })}
                            className="btn-text-action btn-text-action-primary"
                            disabled={b.statut !== 0}
                            title={b.statut !== 0 ? "Ce bureau n'est pas disponible" : undefined}
                          >
                            Affecter →
                          </button>
                        </td>
                      </tr>
                    );
                  })
                )}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
};

export default RechercheIA;
