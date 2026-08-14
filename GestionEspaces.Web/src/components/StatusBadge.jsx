import React from 'react';

const TONE_CLASSES = {
  success: 'bg-success/10 text-success border-l-2 border-success',
  warning: 'bg-warning/10 text-warning border-l-2 border-warning',
  danger: 'bg-danger/10 text-danger border-l-2 border-danger',
  neutral: 'bg-text-secondary/10 text-text-secondary border-l-2 border-text-secondary',
};

/**
 * tone: 'success' | 'warning' | 'danger' | 'neutral'
 */
const StatusBadge = ({ tone = 'neutral', children }) => (
  <span className={`status-tag ${TONE_CLASSES[tone] || TONE_CLASSES.neutral}`}>{children}</span>
);

export default StatusBadge;
