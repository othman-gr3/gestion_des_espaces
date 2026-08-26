import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import EntityImage from '../components/EntityImage';

const MonProfil = () => {
  const [agent, setAgent] = useState(null);
  const [telephone, setTelephone] = useState('');
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [passwordSaving, setPasswordSaving] = useState(false);
  const [passwordError, setPasswordError] = useState('');
  const [passwordSuccess, setPasswordSuccess] = useState('');

  useEffect(() => {
    const fetchProfile = async () => {
      setLoading(true);
      setError('');
      try {
        const response = await api.get('/agents/me/profile');
        setAgent(response.data);
        setTelephone(response.data.telephone || '');
      } catch (err) {
        console.error('Failed to load my profile:', err);
        setError('Impossible de charger votre profil.');
      } finally { setLoading(false); }
    };
    fetchProfile();
  }, []);

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setSaving(true);
    try {
      const response = await api.put('/agents/me/profile', {
        concurrencyToken: agent.concurrencyToken,
        telephone: telephone || null,
      });
      setAgent(response.data);
      setTelephone(response.data.telephone || '');
      setSuccess('Votre numéro de téléphone a été mis à jour.');
    } catch (err) {
      console.error('Profile update error:', err);
      setError(err.response?.data?.detail || "La mise à jour a échoué.");
    } finally { setSaving(false); }
  };

  const handlePasswordSubmit = async (e) => {
    e.preventDefault();
    setPasswordError('');
    setPasswordSuccess('');

    if (newPassword.length < 8) {
      setPasswordError('Le nouveau mot de passe doit contenir au moins 8 caractères.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setPasswordError('La confirmation ne correspond pas au nouveau mot de passe.');
      return;
    }

    setPasswordSaving(true);
    try {
      await api.post('/auth/change-password', { currentPassword, newPassword });
      setPasswordSuccess('Votre mot de passe a été mis à jour.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err) {
      console.error('Password change error:', err);
      setPasswordError(err.response?.data?.detail || 'La mise à jour a échoué.');
    } finally { setPasswordSaving(false); }
  };

  return (
    <div className="max-w-2xl">
      <Breadcrumb items={[{ label: 'Mon espace' }, { label: 'Mon profil' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Mon profil
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">Vos informations administratives sont gérées par l'Administrateur — vous pouvez uniquement corriger votre numéro de téléphone.</p>
      </div>

      {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}
      {success && <div className="mb-4 border-l-[3px] border-success bg-success/5 px-4 py-3 text-[13px] font-medium text-success">{success}</div>}

      {loading ? (
        <div className="text-[13px] text-text-secondary">Chargement...</div>
      ) : !agent ? null : (
        <div className="struct-card p-6">
          <div className="flex items-center gap-4 border-b border-border-subtle pb-5 mb-5">
            <EntityImage src={agent.image} alt={`${agent.prenom} ${agent.nom}`} size={64} rounded="rounded-full" />
            <div>
              <div className="text-[15px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
                {agent.nom.toUpperCase()} {agent.prenom}
              </div>
              {agent.fonction && <div className="text-[12.5px] text-text-secondary mt-0.5">{agent.fonction}</div>}
            </div>
          </div>

          <div className="grid grid-cols-2 gap-5 border-b border-border-subtle pb-5 mb-5">
            <div>
              <div className="field-label">Matricule</div>
              <div className="text-[13px] text-text-primary mt-1" style={{ fontFamily: 'var(--font-mono)' }}>{agent.matricule}</div>
            </div>
            <div>
              <div className="field-label">Nom complet</div>
              <div className="text-[13px] text-text-primary mt-1">{agent.nom.toUpperCase()} {agent.prenom}</div>
            </div>
            <div>
              <div className="field-label">Email</div>
              <div className="text-[13px] text-text-primary mt-1">{agent.email || '—'}</div>
            </div>
            <div>
              <div className="field-label">Fonction</div>
              <div className="text-[13px] text-text-primary mt-1">{agent.fonction || '—'}</div>
            </div>
            <div>
              <div className="field-label">Département</div>
              <div className="text-[13px] text-text-primary mt-1">{agent.departement || '—'}</div>
            </div>
            <div>
              <div className="field-label">Date d'embauche</div>
              <div className="text-[13px] text-text-primary mt-1">{agent.dateEmbauche ? new Date(agent.dateEmbauche).toLocaleDateString('fr-FR') : '—'}</div>
            </div>
          </div>

          <form onSubmit={handleSubmit} className="flex items-end gap-4">
            <div className="flex-1">
              <label className="field-label">Téléphone</label>
              <input
                type="text"
                value={telephone}
                onChange={(e) => setTelephone(e.target.value)}
                className="form-field"
                placeholder="06XXXXXXXX"
                maxLength={30}
              />
            </div>
            <button
              type="submit"
              disabled={saving}
              className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
              style={{ fontFamily: 'var(--font-mono)' }}
            >
              {saving ? 'Enregistrement...' : 'Enregistrer'}
            </button>
          </form>
        </div>
      )}

      <div className="struct-card p-6 mt-6">
        <h3 className="text-[13.5px] font-semibold text-text-primary mb-4" style={{ fontFamily: 'var(--font-display)', fontWeight: 600 }}>
          Changer le mot de passe
        </h3>

        {passwordError && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{passwordError}</div>}
        {passwordSuccess && <div className="mb-4 border-l-[3px] border-success bg-success/5 px-4 py-3 text-[13px] font-medium text-success">{passwordSuccess}</div>}

        <form onSubmit={handlePasswordSubmit} className="space-y-4">
          <div>
            <label className="field-label">Mot de passe actuel</label>
            <input
              type="password"
              value={currentPassword}
              onChange={(e) => setCurrentPassword(e.target.value)}
              className="form-field"
              required
              autoComplete="current-password"
            />
          </div>
          <div className="grid grid-cols-2 gap-5">
            <div>
              <label className="field-label">Nouveau mot de passe</label>
              <input
                type="password"
                value={newPassword}
                onChange={(e) => setNewPassword(e.target.value)}
                className="form-field"
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>
            <div>
              <label className="field-label">Confirmer le nouveau mot de passe</label>
              <input
                type="password"
                value={confirmPassword}
                onChange={(e) => setConfirmPassword(e.target.value)}
                className="form-field"
                required
                minLength={8}
                autoComplete="new-password"
              />
            </div>
          </div>
          <p className="text-[11.5px] text-text-secondary">Minimum 8 caractères.</p>
          <div className="flex items-center justify-end pt-2 border-t border-border-subtle">
            <button
              type="submit"
              disabled={passwordSaving}
              className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
              style={{ fontFamily: 'var(--font-mono)' }}
            >
              {passwordSaving ? 'Enregistrement...' : 'Changer le mot de passe'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default MonProfil;
