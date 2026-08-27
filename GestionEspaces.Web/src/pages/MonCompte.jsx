import React, { useEffect, useState } from 'react';
import api from '../services/api';
import Breadcrumb from '../components/Breadcrumb';
import EntityImage from '../components/EntityImage';
import ImageUploadField from '../components/ImageUploadField';

const MonCompte = () => {
  const [me, setMe] = useState(null);
  const [loading, setLoading] = useState(true);
  const [imageError, setImageError] = useState('');

  const [currentPassword, setCurrentPassword] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [confirmPassword, setConfirmPassword] = useState('');
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  useEffect(() => {
    const fetchMe = async () => {
      setLoading(true);
      try {
        const response = await api.get('/auth/me');
        setMe(response.data);
      } catch (err) {
        console.error('Failed to load account:', err);
      } finally { setLoading(false); }
    };
    fetchMe();
  }, []);

  const handleImageChange = async (url) => {
    setImageError('');
    try {
      const response = await api.put('/auth/me/image', { image: url || null });
      setMe(response.data);
    } catch (err) {
      console.error('Image update error:', err);
      setImageError(err.response?.data?.detail || "La mise à jour de la photo a échoué.");
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');

    if (newPassword.length < 8) {
      setError('Le nouveau mot de passe doit contenir au moins 8 caractères.');
      return;
    }
    if (newPassword !== confirmPassword) {
      setError('La confirmation ne correspond pas au nouveau mot de passe.');
      return;
    }

    setSaving(true);
    try {
      await api.post('/auth/change-password', { currentPassword, newPassword });
      setSuccess('Votre mot de passe a été mis à jour.');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');
    } catch (err) {
      console.error('Password change error:', err);
      setError(err.response?.data?.detail || 'La mise à jour a échoué.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <div className="max-w-2xl">
      <Breadcrumb items={[{ label: 'Mon compte' }]} />

      <div className="border-b-2 border-border-subtle pb-4 mb-6">
        <h2 className="text-xl font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
          Mon compte
        </h2>
        <p className="mt-1 text-[12.5px] text-text-secondary">Vos informations de connexion.</p>
      </div>

      {loading ? (
        <div className="text-[13px] text-text-secondary">Chargement...</div>
      ) : (
        <>
          <div className="struct-card p-6 mb-6">
            <div className="flex items-center gap-4 border-b border-border-subtle pb-5 mb-5">
              <EntityImage src={me?.image} alt={me?.name} size={64} rounded="rounded-full" />
              <div>
                <div className="text-[15px] font-bold text-text-primary" style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}>
                  {me?.name || '—'}
                </div>
                <div className="text-[12.5px] text-text-secondary mt-0.5">{me?.role}</div>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-5 border-b border-border-subtle pb-5 mb-5">
              <div>
                <div className="field-label">Nom</div>
                <div className="text-[13px] text-text-primary mt-1">{me?.name || '—'}</div>
              </div>
              <div>
                <div className="field-label">Rôle</div>
                <div className="text-[13px] text-text-primary mt-1">{me?.role || '—'}</div>
              </div>
              <div className="col-span-2">
                <div className="field-label">Email</div>
                <div className="text-[13px] text-text-primary mt-1">{me?.email || '—'}</div>
              </div>
            </div>

            {imageError && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{imageError}</div>}
            <ImageUploadField
              value={me?.image || ''}
              onChange={handleImageChange}
              alt={me?.name}
              label="Photo de profil (optionnel)"
            />
          </div>

          <div className="struct-card p-6">
            <h3 className="text-[13.5px] font-semibold text-text-primary mb-4" style={{ fontFamily: 'var(--font-display)', fontWeight: 600 }}>
              Changer le mot de passe
            </h3>

            {error && <div className="mb-4 border-l-[3px] border-danger bg-danger/5 px-4 py-3 text-[13px] font-medium text-danger">{error}</div>}
            {success && <div className="mb-4 border-l-[3px] border-success bg-success/5 px-4 py-3 text-[13px] font-medium text-success">{success}</div>}

            <form onSubmit={handleSubmit} className="space-y-4">
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
                  disabled={saving}
                  className="bg-primary px-6 py-2.5 text-[12px] font-semibold uppercase tracking-wider text-white hover:bg-primary-dark transition-colors disabled:opacity-50"
                  style={{ fontFamily: 'var(--font-mono)' }}
                >
                  {saving ? 'Enregistrement...' : 'Changer le mot de passe'}
                </button>
              </div>
            </form>
          </div>
        </>
      )}
    </div>
  );
};

export default MonCompte;
