import React from 'react';

/**
 * Clickable table header cell for client-side column sorting.
 * sortKey/sortDir/onSort come from the parent's local sort state.
 */
const SortableTh = ({ label, column, sortKey, sortDir, onSort, align = 'left', className = '' }) => {
  const isActive = sortKey === column;
  return (
    <th
      className={`px-4 py-2.5 ${align === 'right' ? 'text-right' : 'text-left'} cursor-pointer select-none group ${className}`}
      onClick={() => onSort(column)}
    >
      <span className={`th-label inline-flex items-center gap-1 ${isActive ? 'text-primary' : 'group-hover:text-text-primary'} transition-colors`}>
        {label}
        <span className="text-[9px] opacity-70">{isActive ? (sortDir === 'asc' ? '▲' : '▼') : ''}</span>
      </span>
    </th>
  );
};

export default SortableTh;
