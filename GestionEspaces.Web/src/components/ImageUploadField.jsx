import React, { useRef, useState } from 'react';
import api from '../services/api';
import EntityImage from './EntityImage';

/**
 * Photo field for entity forms: lets the user upload a file (stored server-side under
 * /uploads) or still paste an external URL directly — both write to the same `value`,
 * matching the seed data which already mixes uploaded and external picture URLs.
 */
const ImageUploadField = ({ value, onChange, label = 'Photo (optionnel)', alt }) => {
  const inputRef = useRef(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState('');

  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    e.target.value = '';
    if (!file) return;

    setError('');
    setUploading(true);
    try {
      const body = new FormData();
      body.append('file', file);
      const response = await api.post('/uploads/image', body, {
        headers: { 'Content-Type': 'multipart/form-data' },
      });
      onChange(response.data.url);
    } catch (err) {
      console.error('Upload error:', err);
      setError(err.response?.data?.detail || "Échec de l'envoi du fichier.");
    } finally {
      setUploading(false);
    }
  };

  return (
    <div>
      <label className="field-label">{label}</label>
      <div className="flex items-center gap-3">
        <EntityImage src={value} alt={alt} size={48} rounded="rounded" />
        <div className="flex-1 space-y-2">
          <input
            type="text"
            value={value}
            onChange={(e) => onChange(e.target.value)}
            className="form-field"
            placeholder="https://... (ou choisir un fichier)"
          />
          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              disabled={uploading}
              className="text-[11.5px] font-semibold uppercase tracking-wider text-primary hover:text-primary-dark transition-colors disabled:opacity-50"
              style={{ fontFamily: 'var(--font-mono)' }}
            >
              {uploading ? 'Envoi en cours...' : 'Choisir un fichier'}
            </button>
            {value && (
              <button
                type="button"
                onClick={() => onChange('')}
                className="text-[11.5px] font-medium text-text-secondary hover:text-danger transition-colors"
              >
                Retirer
              </button>
            )}
          </div>
          <input
            ref={inputRef}
            type="file"
            accept="image/jpeg,image/png,image/gif,image/webp"
            onChange={handleFileChange}
            className="hidden"
          />
          {error && <div className="text-[11.5px] text-danger">{error}</div>}
        </div>
      </div>
    </div>
  );
};

export default ImageUploadField;
