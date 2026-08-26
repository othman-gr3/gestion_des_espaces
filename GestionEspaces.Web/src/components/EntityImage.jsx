import React, { useState } from 'react';

/**
 * Renders an entity's Image field as an actual picture. Falls back to a colored
 * initials tile — never a broken-image icon, never nothing — when the field is empty
 * or the URL fails to load (e.g. a filename-only placeholder with no real image behind it).
 */
const EntityImage = ({ src, alt, size = 36, rounded = 'rounded-full' }) => {
  const [failed, setFailed] = useState(false);

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
    <img
      src={src}
      alt={alt || ''}
      onError={() => setFailed(true)}
      className={`object-cover ${rounded} flex-shrink-0`}
      style={{ width: size, height: size }}
    />
  );
};

export default EntityImage;
