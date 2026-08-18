import api from './api';

// ── Referentiel lookups (read-only, used to populate selects/filters) ──────────

export const listAgents = (params = {}) => api.get('/agents', { params: { pageSize: 100, ...params } });
export const listBureaux = (params = {}) => api.get('/bureaux', { params: { pageSize: 100, ...params } });
export const listActifs = (params = {}) => api.get('/actifs', { params: { pageSize: 100, ...params } });
export const listBatiments = (params = {}) => api.get('/batiments', { params: { pageSize: 100, ...params } });
export const listSites = (params = {}) => api.get('/sites', { params: { pageSize: 100, ...params } });

// ── Office (poste) assignments ──────────────────────────────────────────────

export const getOfficeAssignments = (agentId) => api.get(`/agents/${agentId}/office-assignments`);

export const createOfficeAssignment = (agentId, bureauId, dateAffectation, motif = null) =>
  api.post(`/agents/${agentId}/office-assignments`, { bureauId, dateAffectation, motif });

export const closeOfficeAssignment = (agentId, affectationId, dateFin) =>
  api.delete(`/agents/${agentId}/office-assignments/${affectationId}`, { data: { dateFin } });

// ── Asset (actif) assignments ───────────────────────────────────────────────

export const getAssetAssignments = (agentId) => api.get(`/agents/${agentId}/asset-assignments`);

export const createAssetAssignment = (agentId, actifId, dateAffectation) =>
  api.post(`/agents/${agentId}/asset-assignments`, { actifId, dateAffectation });

// etatRetour: 0=Neuf, 1=Bon, 2=ARepairer, 3=HorsService, or null/undefined to leave the asset's état unchanged
export const closeAssetAssignment = (agentId, affectationId, dateFin, etatRetour = null) =>
  api.delete(`/agents/${agentId}/asset-assignments/${affectationId}`, { data: { dateFin, etatRetour } });

/**
 * The API only exposes assignment listings scoped to a single agent — there is no
 * global "all assignments" endpoint. To power the search/history pages we fetch the
 * agent roster once, then fetch each agent's office + asset assignments in parallel
 * and merge the results client-side. Fine at this app's scale (tens of agents);
 * would need a dedicated backend endpoint to stay efficient at larger scale.
 */
export const fetchAllAssignments = async () => {
  const agentsRes = await listAgents();
  const agents = agentsRes.data.items || [];

  const results = await Promise.all(
    agents.map((agent) =>
      Promise.all([getOfficeAssignments(agent.idAgent), getAssetAssignments(agent.idAgent)])
    )
  );

  const posteAffectations = [];
  const actifAffectations = [];

  results.forEach(([officeRes, assetRes], i) => {
    const agent = agents[i];
    (officeRes.data || []).forEach((a) => posteAffectations.push({ ...a, agent }));
    (assetRes.data || []).forEach((a) => actifAffectations.push({ ...a, agent }));
  });

  return { agents, posteAffectations, actifAffectations };
};
