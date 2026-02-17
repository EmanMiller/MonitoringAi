import React from 'react';

function exportToCsv(columns, rows) {
  if (!columns?.length || !rows?.length) return '';
  const header = columns.join(',');
  const body = rows.map((r) => columns.map((c) => {
    const v = r[c] ?? r[c.toLowerCase()] ?? '';
    const s = String(v);
    return s.includes(',') || s.includes('"') ? `"${s.replace(/"/g, '""')}"` : s;
  }).join(',')).join('\n');
  return header + '\n' + body;
}

function ResultsPanel({
  loading,
  error,
  columns = [],
  rows = [],
  rowCount = 0,
  executionTimeMs = 0,
  message = '',
  hasRun = false,
  onExportCsv,
  onVisualize,
}) {
  if (!hasRun && !loading && !error) return null;

  const handleExport = () => {
    const csv = exportToCsv(columns, rows);
    if (!csv) return;
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const a = document.createElement('a');
    a.href = URL.createObjectURL(blob);
    a.download = `query-results-${Date.now()}.csv`;
    a.click();
    URL.revokeObjectURL(a.href);
    onExportCsv?.();
  };

  const displayCols = columns.length ? columns : (rows[0] ? Object.keys(rows[0]) : []);

  return (
    <div className="query-builder-results">
      <div className="qb-results-header">
        <span className="qb-results-meta">
          {loading && <span className="qb-loading">Loading…</span>}
          {!loading && (
            <>
              {rowCount} row{rowCount !== 1 ? 's' : ''}
              {executionTimeMs > 0 && ` · ${executionTimeMs}ms`}
            </>
          )}
        </span>
        {!loading && rows.length > 0 && (
          <div className="qb-results-actions">
            <button type="button" className="qb-results-btn" onClick={handleExport}>
              Export CSV
            </button>
            <button type="button" className="qb-results-btn" onClick={onVisualize}>
              Visualize
            </button>
          </div>
        )}
      </div>
      {error && <div className="qb-results-error">{error}</div>}
      {loading && (
        <div className="qb-results-loading">
          <div className="qb-spinner" />
        </div>
      )}
      {!loading && rows.length === 0 && message && (
        <div className="qb-results-empty-msg">{message}</div>
      )}
      {!loading && rows.length > 0 && (
        <div className="qb-results-table-wrap">
          <table className="qb-results-table">
            <thead>
              <tr>
                {displayCols.map((c) => (
                  <th key={c}>{c}</th>
                ))}
              </tr>
            </thead>
            <tbody>
              {rows.map((row, i) => (
                <tr key={i}>
                  {displayCols.map((col) => (
                    <td key={col}>{String(row[col] ?? row[col.toLowerCase()] ?? '')}</td>
                  ))}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

export default ResultsPanel;
