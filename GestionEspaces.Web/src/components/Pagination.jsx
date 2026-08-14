import React from 'react';

const Pagination = ({ page, pageSize, totalCount, onPageChange }) => {
  if (totalCount <= pageSize) return null;

  return (
    <div className="flex items-center justify-between border-t border-border-subtle pt-3 mt-4">
      <button
        disabled={page === 1}
        onClick={() => onPageChange(Math.max(page - 1, 1))}
        className="text-[12.5px] font-medium text-primary hover:text-primary-dark disabled:opacity-40 transition-colors"
      >
        ← Précédent
      </button>
      <span className="text-[11.5px] text-text-secondary" style={{ fontFamily: 'var(--font-mono)' }}>
        Page {page} / {Math.max(Math.ceil(totalCount / pageSize), 1)} · {totalCount} résultat{totalCount > 1 ? 's' : ''}
      </span>
      <button
        disabled={page * pageSize >= totalCount}
        onClick={() => onPageChange(page + 1)}
        className="text-[12.5px] font-medium text-primary hover:text-primary-dark disabled:opacity-40 transition-colors"
      >
        Suivant →
      </button>
    </div>
  );
};

export default Pagination;
