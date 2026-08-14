import React from 'react';

/**
 * Slide-in side panel used for create/edit forms across the app.
 * accent: 'primary' | 'accent' — controls the border/header accent color.
 */
const Drawer = ({ open, onClose, eyebrow, title, accent = 'primary', width = 'max-w-lg', children }) => {
  if (!open) return null;

  const accentBorder = accent === 'accent' ? 'border-accent' : 'border-primary';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-end bg-black/30" onClick={onClose}>
      <div
        className={`modal-slide-in h-full w-full ${width} bg-surface-bg border-l-2 ${accentBorder} overflow-y-auto`}
        onClick={(e) => e.stopPropagation()}
      >
        <div className={`flex items-center justify-between border-b-2 ${accentBorder} px-6 py-4 bg-neutral-bg sticky top-0 z-10`}>
          <div>
            {eyebrow && <div className="th-label mb-0.5">{eyebrow}</div>}
            <h3
              className="text-[16px] font-bold text-text-primary"
              style={{ fontFamily: 'var(--font-display)', fontWeight: 700 }}
            >
              {title}
            </h3>
          </div>
          <button
            onClick={onClose}
            className="text-text-secondary hover:text-text-primary transition-colors text-xl w-8 h-8 flex items-center justify-center flex-shrink-0"
            aria-label="Fermer"
          >
            ✕
          </button>
        </div>
        {children}
      </div>
    </div>
  );
};

export default Drawer;
