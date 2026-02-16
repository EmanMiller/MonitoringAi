import React, { useState, useEffect, useCallback } from 'react';
import { getLogMappings, searchSavedQueries, askQuery } from '../services/api';
import { SEARCH_QUERY_MAX_LENGTH } from '../utils/sanitize';

function QuickQueryPopup({ isOpen, onClose, onQueryReady }) {
  const [input, setInput] = useState('');
  const [debouncedInput, setDebouncedInput] = useState('');
  const [mappingMatch, setMappingMatch] = useState(null);
  const [savedMatch, setSavedMatch] = useState(null);
  const [asking, setAsking] = useState(false);
  const [error, setError] = useState(null);

  useEffect(() => {
    if (!isOpen) return;
    const trimmed = input.trim().slice(0, SEARCH_QUERY_MAX_LENGTH);
    const t = setTimeout(() => setDebouncedInput(trimmed), 300);
    return () => clearTimeout(t);
  }, [isOpen, input]);

  const runSearch = useCallback(async (term) => {
    if (!term) {
      setMappingMatch(null);
      setSavedMatch(null);
      return;
    }
    const termLower = term.toLowerCase();
    try {
      const [mappings, saved] = await Promise.all([
        getLogMappings(),
        searchSavedQueries(term),
      ]);
      const activeMappings = mappings.filter((m) => m.isActive);
      const keyMatch = activeMappings.find((m) => m.key.toLowerCase() === termLower || termLower.includes(m.key.toLowerCase()));
      if (keyMatch) {
        setMappingMatch(keyMatch);
      } else {
        setMappingMatch(null);
      }
      if (saved && saved.length > 0) {
        const exact = saved.find((s) => s.name.toLowerCase() === termLower);
        setSavedMatch(exact || saved[0]);
      } else {
        setSavedMatch(null);
      }
    } catch {
      setMappingMatch(null);
      setSavedMatch(null);
    }
  }, []);

  useEffect(() => {
    runSearch(debouncedInput);
  }, [debouncedInput, runSearch]);

  const handleAskAi = async () => {
    if (!input.trim()) return;
    setError(null);
    setAsking(true);
    try {
      const { queryText } = await askQuery(input.trim());
      onQueryReady?.(queryText);
      onClose?.();
    } catch (e) {
      setError(e.response?.data?.details || e.message || 'Request failed');
    } finally {
      setAsking(false);
    }
  };

  const handleUseMatch = (queryText) => {
    onQueryReady?.(queryText);
    onClose?.();
  };

  if (!isOpen) return null;

  const hasInstantMatch = (mappingMatch && mappingMatch.value) || (savedMatch && savedMatch.queryText);
  const instantQuery = savedMatch?.queryText ?? mappingMatch?.value ?? null;

  return (
    <div className="modal-overlay quick-query-overlay" onClick={onClose}>
      <div className="quick-query-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Quick Query</h2>
          <button type="button" className="close-button" onClick={onClose} aria-label="Close">&times;</button>
        </div>
        <div className="quick-query-body">
          <input
            type="text"
            className="quick-query-input"
            value={input}
            onChange={(e) => setInput(e.target.value.slice(0, SEARCH_QUERY_MAX_LENGTH))}
            placeholder="e.g. prod errors, qa-www latency"
            maxLength={SEARCH_QUERY_MAX_LENGTH}
            autoFocus
          />
          {debouncedInput && (
            <div className="quick-query-matches">
              {instantQuery && (
                <div className="quick-query-instant">
                  <span className="quick-query-instant-label">Instant match:</span>
                  <code className="quick-query-instant-value">{instantQuery}</code>
                  <button type="button" className="button-primary" onClick={() => handleUseMatch(instantQuery)}>
                    Use this query
                  </button>
                </div>
              )}
              {!hasInstantMatch && (
                <div className="quick-query-ask">
                  <p>No exact match. Ask AI to generate a Sumo Logic query.</p>
                  <button
                    type="button"
                    className="button-primary"
                    onClick={handleAskAi}
                    disabled={asking}
                  >
                    {asking ? 'Asking…' : 'Ask AI'}
                  </button>
                </div>
              )}
              {hasInstantMatch && (
                <div className="quick-query-ask">
                  <button
                    type="button"
                    className="button-secondary"
                    onClick={handleAskAi}
                    disabled={asking}
                  >
                    {asking ? 'Asking…' : 'Ask AI instead'}
                  </button>
                </div>
              )}
            </div>
          )}
          {error && <p className="quick-query-error">{error}</p>}
        </div>
      </div>
    </div>
  );
}

export default QuickQueryPopup;
