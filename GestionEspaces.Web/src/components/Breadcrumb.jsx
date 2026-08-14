import React from 'react';
import { Link } from 'react-router-dom';

/**
 * items: [{ label, href? }] — the last item is rendered as the current page (no link).
 */
const Breadcrumb = ({ items }) => {
  if (!items || items.length === 0) return null;

  return (
    <nav
      className="flex items-center flex-wrap gap-1.5 text-[11px] uppercase tracking-[0.08em] mb-3"
      style={{ fontFamily: 'var(--font-mono)' }}
      aria-label="Fil d'ariane"
    >
      {items.map((item, i) => {
        const isLast = i === items.length - 1;
        return (
          <span key={i} className="flex items-center gap-1.5">
            {i > 0 && <span className="text-text-secondary/50">/</span>}
            {item.href && !isLast ? (
              <Link to={item.href} className="text-text-secondary hover:text-primary transition-colors">
                {item.label}
              </Link>
            ) : (
              <span className={isLast ? 'text-primary font-semibold' : 'text-text-secondary'}>{item.label}</span>
            )}
          </span>
        );
      })}
    </nav>
  );
};

export default Breadcrumb;
