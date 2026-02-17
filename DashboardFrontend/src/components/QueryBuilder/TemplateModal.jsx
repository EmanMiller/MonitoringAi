import React from 'react';
import { TEMPLATES, TEMPLATE_QUERIES } from './queryBuilderLogic';

function TemplateModal({ isOpen, onClose, onSelect }) {
  if (!isOpen) return null;

  const handleSelect = (id) => {
    const query = TEMPLATE_QUERIES[id];
    if (query) onSelect?.(query);
    onClose?.();
  };

  return (
    <div className="modal-overlay query-builder-template-overlay" onClick={onClose}>
      <div className="query-builder-template-modal modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Load Template</h2>
          <button type="button" className="close-button" onClick={onClose} aria-label="Close">&times;</button>
        </div>
        <div className="qb-template-grid">
          {TEMPLATES.map((t) => (
            <button
              key={t.id}
              type="button"
              className="qb-template-card"
              onClick={() => handleSelect(t.id)}
            >
              <span className="qb-template-icon">{t.icon}</span>
              <h4 className="qb-template-name">{t.name}</h4>
              <p className="qb-template-desc">{t.description}</p>
              <span className="qb-template-badge">{t.category}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
}

export default TemplateModal;
