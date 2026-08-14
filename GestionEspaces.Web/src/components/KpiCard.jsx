import React from 'react';

/**
 * Sober back-office KPI tile — denser than a marketing stat card.
 */
const KpiCard = ({ value, label, tag }) => (
  <div className="struct-card px-5 py-4">
    <div className="th-label mb-2">{tag}</div>
    <div
      className="text-3xl font-extrabold text-primary leading-none mb-1.5"
      style={{ fontFamily: 'var(--font-display)', fontWeight: 800 }}
    >
      {value}
    </div>
    <div className="text-[12.5px] text-text-secondary">{label}</div>
  </div>
);

export default KpiCard;
