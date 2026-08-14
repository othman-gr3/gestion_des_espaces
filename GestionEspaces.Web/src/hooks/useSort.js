import { useMemo, useState } from 'react';

/**
 * Client-side column sort for a row array already fetched from the API.
 * getValue(row, column) must return a comparable primitive for the given column key.
 */
export default function useSort(rows, getValue, initialColumn = null, initialDir = 'asc') {
  const [sortKey, setSortKey] = useState(initialColumn);
  const [sortDir, setSortDir] = useState(initialDir);

  const onSort = (column) => {
    if (sortKey === column) {
      setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
    } else {
      setSortKey(column);
      setSortDir('asc');
    }
  };

  const sortedRows = useMemo(() => {
    if (!sortKey) return rows;
    const copy = [...rows];
    copy.sort((a, b) => {
      const va = getValue(a, sortKey);
      const vb = getValue(b, sortKey);
      if (va == null && vb == null) return 0;
      if (va == null) return 1;
      if (vb == null) return -1;
      if (typeof va === 'number' && typeof vb === 'number') return va - vb;
      return String(va).localeCompare(String(vb), 'fr', { sensitivity: 'base' });
    });
    if (sortDir === 'desc') copy.reverse();
    return copy;
  }, [rows, sortKey, sortDir, getValue]);

  return { sortedRows, sortKey, sortDir, onSort };
}
