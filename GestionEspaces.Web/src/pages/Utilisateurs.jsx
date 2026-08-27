import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import Drawer from '../components/Drawer';
import StatusBadge from '../components/StatusBadge';
import EntityImage from '../components/EntityImage';

const ROLE_TONE = {
  Administrateur: 'neutral',
  Gestionnaire: 'warning',
  Agent: 'success',
};

const Utilisateurs = () => {
  const [users, setUsers] = useState([]);
  const [agents, setAgents] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [savingRoleFor, setSavingRoleFor] = useState(null);

  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [formRole, setFormRole] = useState('Agent');
  const [selectedAgentId, setSelectedAgentId] = useState('');
  const [manualEmail, setManualEmail] = useState('');
  const [manualName, setManualName] = useState('');
  const [password, setPassword] = useState('');
  const [formError, setFormError] = useState('');
  const [saving, setSaving] = useState(false);

  useEffect(() => { fetchAll(); }, []);

  const fetchAll = async () => {
    setLoading(true);
    setError('');
    try {
      const [usersRes, agentsRes] = await Promise.all([
        api.get('/users'),
        api.get('/agents', { params: { pageSize: 100 } }),
      ]);
      setUsers(usersRes.data || []);
      setAgents(agentsRes.data.items || []);
    } catch (err) {
      console.error('Failed to load users:', err);
      setError('Impossible de charger les comptes.');
    } finally { setLoading(false); }
  };

  // Agents that have an email (required for a login) and don't already have an account.
  const existingEmails = new Set(users.map((u) => u.email.toLowerCase()));
  const eligibleAgents = agents.filter((a) => a.email && !existingEmails.has(a.email.toLowerCase()));

  const handleRoleChange = async (user, newRole) => {
    if (newRole === user.role) return;
    setSavingRoleFor(user.idAppUser);
    try {
      await api.put(`/users/${user.idAppUser}/role`, { role: newRole });
      fetchAll();
    } catch (err) {
      console.error('Role change error:', err);
      alert(err.response?.data?.detail || 'Impossible de modifier ce rôle.');
    } finally { setSavingRoleFor(null); }
  };

  const handleOpenDrawer = () => {
    setFormRole('Agent');
    setSelectedAgentId('');
    setManualEmail('');
    setManualName('');
    setPassword('');
    setFormError('');
    setIsDrawerOpen(true);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setFormError('');

    const selectedAgent = agents.find((a) => String(a.idAgent) === String(selectedAgentId));
    const email = formRole === 'Agent' ? selectedAgent?.email : manualEmail;
    const name = formRole === 'Agent' ? `${selectedAgent?.prenom} ${selectedAgent?.nom}` : manualName;

    if (formRole === 'Agent' && !selectedAgent) {
      setFormError('Sélectionnez une fiche agent.');
      return;
    }
    if (formRole === 'Gestionnaire' && (!manualEmail || !manualName)) {
      setFormError('L\'email et le nom sont obligatoires.');
      return;
    }
    if (password.length < 8) {
      setFormError('Le mot de passe doit contenir au moins 8 caractères.');
      return;
    }

    setSaving(true);
    try {
      await api.post('/users', { email, name, role: formRole, password });
      setIsDrawerOpen(false);
      fetchAll();
    } catch (err) {
      console.error('Create account error:', err);
      setFormError(err.response?.data?.detail || "La création du compte a échoué.");
    } finally { setSaving(false); }
  };

  return (
    <div>
      <Breadcrumb items={[{ label: 'Sécurité' }, { label: 'Utilisateurs' }]} />

      <div className="flex items-center justify-between border-b-2 border-primary bg-surface-bg px-4 py-2.5 mb-4">
        <div className="text-[12.5px] text-text-secondary">Comptes de connexion et leur rôle. Le rôle Administrateur ne peut pas être attribué ici.</div>
        <button onClick={handleOpenDrawer} className="ml-4 bg-primary px-4 py-1.5 text-[11.5px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors" style={{ fontFamily: 'var(--font-mono)' }}>
          + Nouveau compte
        </button>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}

      <div className="border border-border-subtle bg-surface-bg overflow-hidden overflow-x-auto">
        <table className="min-w-full">
          <thead>
            <tr className="border-b-2 border-primary bg-neutral-bg">
              <th className="px-4 py-2.5 text-left"><span className="th-label">Nom</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Email</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Rôle</span></th>
              <th className="px-4 py-2.5 text-left"><span className="th-label">Changer le rôle</span></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-border-subtle">
            {loading ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Chargement des comptes...</td></tr>
            ) : users.length === 0 ? (
              <tr><td colSpan={4} className="px-4 py-8 text-center text-[12.5px] text-text-secondary">Aucun compte trouvé.</td></tr>
            ) : (
              users.map((u) => (
                <tr key={u.idAppUser} className="hover:bg-neutral-bg/60 transition-colors">
                  <td className="whitespace-nowrap px-4 py-2.5">
                    <div className="flex items-center gap-3">
                      <EntityImage src={u.image} alt={u.name} size={32} />
                      <span className="text-[13px] font-semibold text-text-primary">{u.name}</span>
                    </div>
                  </td>
                  <td className="whitespace-nowrap px-4 py-2.5 text-[12.5px] text-text-secondary">{u.email}</td>
                  <td className="whitespace-nowrap px-4 py-2.5"><StatusBadge tone={ROLE_TONE[u.role]}>{u.role}</StatusBadge></td>
                  <td className="whitespace-nowrap px-4 py-2.5">
                    {u.role === 'Administrateur' ? (
                      <span className="text-[11.5px] text-text-secondary">—</span>
                    ) : (
                      <select
                        value={u.role}
                        disabled={savingRoleFor === u.idAppUser}
                        onChange={(e) => handleRoleChange(u, e.target.value)}
                        className="form-field w-40"
                      >
                        <option value="Gestionnaire">Gestionnaire</option>
                        <option value="Agent">Agent</option>
                      </select>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <Drawer
        open={isDrawerOpen}
        onClose={() => setIsDrawerOpen(false)}
        eyebrow="Création"
        title="Nouveau compte"
      >
        {formError && <div className="mx-6 mt-5 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{formError}</div>}
        <form onSubmit={handleSubmit} className="p-6 space-y-5">
          <div>
            <label className="field-label">Rôle</label>
            <select value={formRole} onChange={(e) => setFormRole(e.target.value)} className="form-field">
              <option value="Agent">Agent</option>
              <option value="Gestionnaire">Gestionnaire</option>
            </select>
          </div>

          {formRole === 'Agent' ? (
            <div>
              <label className="field-label">Fiche agent</label>
              <select value={selectedAgentId} onChange={(e) => setSelectedAgentId(e.target.value)} className="form-field" required>
                <option value="">Sélectionner...</option>
                {eligibleAgents.map((a) => (
                  <option key={a.idAgent} value={a.idAgent}>{a.prenom} {a.nom} — {a.email}</option>
                ))}
              </select>
              {eligibleAgents.length === 0 && (
                <p className="mt-1.5 text-[11.5px] text-text-secondary">Tous les agents ayant un email ont déjà un compte.</p>
              )}
            </div>
          ) : (
            <div className="grid grid-cols-2 gap-5">
              <div>
                <label className="field-label">Nom</label>
                <input type="text" value={manualName} onChange={(e) => setManualName(e.target.value)} className="form-field" required />
              </div>
              <div>
                <label className="field-label">Email</label>
                <input type="email" value={manualEmail} onChange={(e) => setManualEmail(e.target.value)} className="form-field" required />
              </div>
            </div>
          )}

          <div>
            <label className="field-label">Mot de passe initial</label>
            <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} className="form-field" required minLength={8} autoComplete="new-password" />
            <p className="mt-1.5 text-[11.5px] text-text-secondary">Minimum 8 caractères. L'utilisateur pourra le changer depuis son propre compte.</p>
          </div>

          <div className="flex items-center justify-end gap-4 pt-4 border-t border-border-subtle">
            <button type="button" onClick={() => setIsDrawerOpen(false)} className="text-[13px] font-medium text-text-secondary hover:text-text-primary transition-colors">Annuler</button>
            <button type="submit" disabled={saving} className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50" style={{ fontFamily: 'var(--font-mono)' }}>
              {saving ? 'Création...' : 'Créer le compte'}
            </button>
          </div>
        </form>
      </Drawer>
    </div>
  );
};

export default Utilisateurs;
