import React, { useEffect, useState } from 'react';

/**
 * Renders an entity's Image field as an actual picture. Falls back to a colored
 * initials tile — never a broken-image icon, never nothing — when the field is empty
 * or the URL fails to load (e.g. a filename-only placeholder with no real image behind it).
 * A real image is clickable and opens an enlarged lightbox preview.
 */
const EntityImage = ({ src, alt, size = 36, rounded = 'rounded-full' }) => {
  const [failed, setFailed] = useState(false);
  const [expanded, setExpanded] = useState(false);

  useEffect(() => {
    if (!expanded) return;
    const onKeyDown = (e) => { if (e.key === 'Escape') setExpanded(false); };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [expanded]);

  const initials = (alt || '')
    .trim()
    .split(/\s+/)
    .filter(Boolean)
    .slice(0, 2)
    .map((word) => word[0])
    .join('')
    .toUpperCase();

  if (!src || failed) {
    return (
      <div
        className={`flex items-center justify-center bg-primary/10 text-primary font-semibold ${rounded} flex-shrink-0`}
        style={{ width: size, height: size, fontSize: Math.max(size * 0.38, 10) }}
      >
        {initials || '?'}
      </div>
    );
  }

  return (
    <>
      <img
        src={src}
        alt={alt || ''}
        onError={() => setFailed(true)}
        onClick={() => setExpanded(true)}
        className={`object-cover ${rounded} flex-shrink-0 cursor-zoom-in hover:opacity-80 transition-opacity`}
        style={{ width: size, height: size }}
      />

      {expanded && (
        <div
          className="fixed inset-0 z-50 flex items-center justify-center bg-black/80 p-8"
          onClick={() => setExpanded(false)}
        >
          <button
            type="button"
            onClick={() => setExpanded(false)}
            className="absolute top-5 right-6 text-white/70 hover:text-white text-3xl leading-none"
            aria-label="Fermer"
          >
            ×
          </button>
          <img
            src={src}
            alt={alt || ''}
            className="max-h-full max-w-full object-contain shadow-2xl"
            onClick={(e) => e.stopPropagation()}
          />
          {alt && (
            <div className="absolute bottom-6 left-1/2 -translate-x-1/2 text-white/90 text-[13px] font-medium" style={{ fontFamily: 'var(--font-display)' }}>
              {alt}
            </div>
          )}
        </div>
      )}
    </>
  );
};

export default EntityImage;
