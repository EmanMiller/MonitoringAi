import React, { useState, useCallback, useEffect } from 'react';
import {
  runQuery,
  explainQuery,
  createQueryLibraryItem,
} from '../../services/api';
import {
  visualToQuery,
  parseQueryToVisual,
} from './queryBuilderLogic';
import VisualMode from './VisualMode';
import CodeMode from './CodeMode';
import PreviewMode from './PreviewMode';
import ResultsPanel from './ResultsPanel';
import TemplateModal from './TemplateModal';

const MODES = ['visual', 'code', 'preview'];

function QueryBuilder() {
  const [mode, setMode] = useState('visual');
  const [visual, setVisual] = useState({
    filters: [{ field: '', operator: '=', value: '' }],
    groupBy: '',
    aggFunction: 'count',
    aggField: '',
    timeRange: '1h',
    timeStart: null,
    timeEnd: null,
    limit: 100,
  });
  const [query, setQuery] = useState('* | limit 100');
  const [explanation, setExplanation] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState(null);
  const [results, setResults] = useState({
    rows: [],
    columns: [],
    rowCount: 0,
    executionTimeMs: 0,
  });
  const [hasRun, setHasRun] = useState(false);
  const [templateModalOpen, setTemplateModalOpen] = useState(false);

  const syncVisualToQuery = useCallback(() => {
    setQuery(visualToQuery(visual));
  }, [visual]);

  const syncQueryToVisual = useCallback(() => {
    setVisual(parseQueryToVisual(query));
  }, [query]);

  useEffect(() => {
    if (mode === 'visual') syncVisualToQuery();
  }, [mode, visual, syncVisualToQuery]);

  useEffect(() => {
    if (mode === 'code') {
      // Don't overwrite query on every visual change when in code mode
    }
  }, [mode]);

  const handleModeChange = (m) => {
    if (m === mode) return;
    if (mode === 'visual' && m === 'code') syncVisualToQuery();
    if (mode === 'code' && m === 'visual') syncQueryToVisual();
    if (m === 'preview') {
      const q = mode === 'visual' ? visualToQuery(visual) : query;
      setQuery(q);
      explainQuery(q).then((d) => setExplanation(d.explanation || d.message || '')).catch(() => setExplanation(''));
    }
    setMode(m);
  };

  const handleVisualChange = (v) => {
    setVisual(v);
    if (mode === 'preview') setQuery(visualToQuery(v));
  };

  const handleQueryChange = (q) => {
    setQuery(q);
  };

  const handleCodeBlur = () => {
    if (mode === 'code') syncQueryToVisual();
  };

  const handleRun = useCallback(async () => {
    const q = mode === 'visual' ? visualToQuery(visual) : query;
    if (!q?.trim()) return;
    setError(null);
    setHasRun(true);
    setLoading(true);
    try {
      const timeRange = visual.timeRange === 'custom' ? '24h' : visual.timeRange || '1h';
      const limit = visual.limit ?? 100;
      const data = await runQuery(q, timeRange, limit);
      setResults({
        rows: data.rows || [],
        columns: data.columns || [],
        rowCount: data.rowCount ?? (data.rows?.length ?? 0),
        executionTimeMs: data.executionTimeMs ?? 0,
        message: data.message,
      });
    } catch (e) {
      setError(e.response?.data?.details || e.message || 'Query failed');
      setResults({ rows: [], columns: [], rowCount: 0, executionTimeMs: 0 });
    } finally {
      setLoading(false);
    }
  }, [mode, visual, query]);

  useEffect(() => {
    const onKeyDown = (e) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
        e.preventDefault();
        handleRun();
      }
    };
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [handleRun]);

  const handleSave = async () => {
    const q = mode === 'visual' ? visualToQuery(visual) : query;
    if (!q?.trim()) return;
    try {
      await createQueryLibraryItem({
        key: `Query ${new Date().toLocaleString()}`,
        value: q,
        category: 'Query Builder',
      });
    } catch (e) {
      setError(e.response?.data?.details || e.message || 'Save failed');
    }
  };

  const handleClear = () => {
    setVisual({
      filters: [{ field: '', operator: '=', value: '' }],
      groupBy: '',
      aggFunction: 'count',
      aggField: '',
      timeRange: '1h',
      timeStart: null,
      timeEnd: null,
      limit: 100,
    });
    setQuery('* | limit 100');
    setExplanation('');
    setError(null);
    setResults({ rows: [], columns: [], rowCount: 0, executionTimeMs: 0 });
    setHasRun(false);
  };

  const handleLoadTemplate = (templateQuery) => {
    setQuery(templateQuery);
    setVisual(parseQueryToVisual(templateQuery));
    setMode('code');
  };

  const displayQuery = mode === 'visual' ? visualToQuery(visual) : query;

  return (
    <div className="query-builder">
      <div className="qb-mode-toggle">
        {MODES.map((m) => (
          <button
            key={m}
            type="button"
            className={`qb-mode-btn ${mode === m ? 'active' : ''}`}
            onClick={() => handleModeChange(m)}
          >
            {m.charAt(0).toUpperCase() + m.slice(1)}
          </button>
        ))}
      </div>

      <div className="qb-panel">
        {mode === 'visual' && (
          <VisualMode visual={visual} onChange={handleVisualChange} />
        )}
        {mode === 'code' && (
          <CodeMode
            query={query}
            onChange={handleQueryChange}
            onBlur={handleCodeBlur}
          />
        )}
        {mode === 'preview' && (
          <PreviewMode
            query={displayQuery}
            explanation={explanation}
          />
        )}
      </div>

      <div className="qb-actions">
        <button type="button" className="qb-run button-primary" onClick={handleRun}>
          ▶ Run Query
        </button>
        <button type="button" className="button-secondary" onClick={handleSave}>
          💾 Save Query
        </button>
        <button type="button" className="button-secondary" onClick={handleClear}>
          🗑️ Clear
        </button>
        <button type="button" className="button-secondary" onClick={() => setTemplateModalOpen(true)}>
          📋 Load Template
        </button>
      </div>

      <ResultsPanel
        loading={loading}
        error={error}
        hasRun={hasRun}
        columns={results.columns}
        rows={results.rows}
        rowCount={results.rowCount}
        executionTimeMs={results.executionTimeMs}
        message={results.message}
      />

      <TemplateModal
        isOpen={templateModalOpen}
        onClose={() => setTemplateModalOpen(false)}
        onSelect={handleLoadTemplate}
      />
    </div>
  );
}

export default QueryBuilder;
