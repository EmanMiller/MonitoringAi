import React, { useState } from 'react';

function PreviewMode({ query, explanation, onCopy }) {
  const [copied, setCopied] = useState(false);

  const handleCopy = () => {
    navigator.clipboard?.writeText(query || '').then(() => {
      setCopied(true);
      onCopy?.();
      setTimeout(() => setCopied(false), 1500);
    });
  };

  return (
    <div className="query-builder-preview">
      <div className="qb-preview-query-section">
        <div className="qb-preview-header">
          <span>Generated Query</span>
          <button
            type="button"
            className={`qb-copy-btn ${copied ? 'copied' : ''}`}
            onClick={handleCopy}
          >
            {copied ? '✓ Copied' : 'Copy'}
          </button>
        </div>
        <pre className="qb-preview-query">{query || '— No query yet —'}</pre>
      </div>
      {explanation && (
        <div className="qb-preview-explanation">
          <h4>What it does</h4>
          <p>{explanation}</p>
        </div>
      )}
    </div>
  );
}

export default PreviewMode;
