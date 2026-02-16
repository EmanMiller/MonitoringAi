import React, { useState, useEffect, useCallback, useRef } from 'react';
import Header from './Header';
import {
  getAllQueryLibrary,
  searchQueryLibrary,
  incrementQueryUsage,
} from '../services/api';

const CATEGORY_ORDER = [
  'Browse Product',
  'Browse Path',
  'Account',
  'Checkout',
  'Gift Registry',
  'API',
];

/** Normalize QueryLibrary item (key/value) to common shape (name/queryText) */
function normalizeItem(item) {
  if (!item) return null;
  const tags = item.tags ?? (() => {
    try { return JSON.parse(item.tagsJson || '[]') || []; } catch { return []; }
  })();
  return {
    id: item.id,
    name: item.key ?? item.name,
    queryText: item.value ?? item.queryText,
    category: item.category ?? '',
    tags: Array.isArray(tags) ? tags.join(', ') : (tags || ''),
    usageCount: item.usageCount ?? 0,
  };
}

function Highlight({ text, term }) {
  if (!term || !text) return text;
  const words = term.split(/\s+/).filter(Boolean).map((w) => w.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'));
  if (words.length === 0) return text;
  const re = new RegExp(`(${words.join('|')})`, 'gi');
  const parts = text.split(re);
  return parts.map((part, i) =>
    i % 2 === 1 ? <mark key={i} className="common-qa-highlight">{part}</mark> : part
  );
}

function QueryCard({ query, searchTerm, onCopy, onViewFull }) {
  const [expanded, setExpanded] = useState(false);
  const key = query.name || 'Untitled query';
  const previewLines = (query.queryText || '').split('\n').filter(Boolean);
  const preview = previewLines.slice(0, 2).join('\n');
  const hasMore = previewLines.length > 2;
  const showPreview = expanded ? (query.queryText || '') : (preview + (hasMore ? '\n...' : ''));

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(query.queryText || '');
      onCopy();
      await incrementQueryUsage(query.id);
    } catch (_) {}
  };

  return (
    <div className="common-qa-card">
      <div className="common-qa-card-header">
        {query.category && (
          <span className="common-qa-badge" data-category={query.category}>
            {query.category}
          </span>
        )}
        <h3 className="common-qa-card-title">
          <Highlight text={key} term={searchTerm} />
        </h3>
      </div>
      <pre className="common-qa-query-preview">
        {showPreview}
        {!expanded && hasMore && (
          <button
            type="button"
            className="common-qa-show-more"
            onClick={() => setExpanded(true)}
          >
            Show More
          </button>
        )}
      </pre>
      <div className="common-qa-card-actions">
        <button type="button" className="common-qa-btn-copy" onClick={handleCopy} title="Copy query">
          📋 Copy Query
        </button>
        <button type="button" className="common-qa-btn-view" onClick={() => onViewFull(query)}>
          View Full Query
        </button>
      </div>
    </div>
  );
}

function CommonQAPage() {
  const [allQueries, setAllQueries] = useState([]);
  const [popularQueries, setPopularQueries] = useState([]); // first 5 from library
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [searchResult, setSearchResult] = useState({ queries: [], total: 0 });
  const [searching, setSearching] = useState(false);
  const [viewMode, setViewMode] = useState('browse'); // 'browse' | 'table'
  const [expandedCategories, setExpandedCategories] = useState({});
  const [toast, setToast] = useState(null);
  const [requestModalOpen, setRequestModalOpen] = useState(false);
  const [fullQueryModal, setFullQueryModal] = useState(null);
  const [focusedIndex, setFocusedIndex] = useState(-1);
  const resultListRef = useRef(null);

  const loadLibrary = useCallback(async () => {
    try {
      const all = await getAllQueryLibrary();
      const list = (Array.isArray(all) ? all : []).map(normalizeItem).filter(Boolean);
      setAllQueries(list);
      setPopularQueries(list.slice(0, 5));
    } catch (_) {
      setAllQueries([]);
      setPopularQueries([]);
    }
  }, []);

  useEffect(() => {
    loadLibrary();
  }, [loadLibrary]);

  useEffect(() => {
    const t = setTimeout(() => setDebouncedSearch(searchInput.trim()), 300);
    return () => clearTimeout(t);
  }, [searchInput]);

  useEffect(() => {
    if (!debouncedSearch) {
      setSearchResult({ queries: [], total: 0 });
      setSearching(false);
      return;
    }
    let cancelled = false;
    setSearching(true);
    searchQueryLibrary(debouncedSearch)
      .then((res) => {
        if (cancelled) return;
        const list = Array.isArray(res) ? res : (res?.queries ?? []);
        const queries = list.map(normalizeItem).filter(Boolean);
        setSearchResult({ queries, total: queries.length });
      })
      .catch(() => {
        if (!cancelled) setSearchResult({ queries: [], total: 0 });
      })
      .finally(() => {
        if (!cancelled) setSearching(false);
      });
    return () => { cancelled = true; };
  }, [debouncedSearch]);

  const showToast = (message) => {
    setToast(message);
    setTimeout(() => setToast(null), 2500);
  };

  const displayQueries = debouncedSearch ? searchResult.queries : [];
  const hasSearch = !!debouncedSearch;
  const byCategory = React.useMemo(() => {
    const map = {};
    CATEGORY_ORDER.forEach((c) => { map[c] = []; });
    map['Other'] = [];
    allQueries.forEach((q) => {
      const cat = q.category && CATEGORY_ORDER.includes(q.category) ? q.category : 'Other';
      map[cat].push(q);
    });
    if (map['Other'].length === 0) delete map['Other'];
    return map;
  }, [allQueries]);

  const categoryList = [
    ...CATEGORY_ORDER.filter((c) => (byCategory[c]?.length ?? 0) > 0),
    ...(byCategory['Other']?.length ? ['Other'] : []),
  ];

  const toggleCategory = (cat) => {
    setExpandedCategories((prev) => ({ ...prev, [cat]: !prev[cat] }));
  };

  const handleKeyDown = (e) => {
    if (!hasSearch || !resultListRef.current) return;
    const items = resultListRef.current.querySelectorAll('[data-result-index]');
    const len = items.length;
    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setFocusedIndex((i) => (i < len - 1 ? i + 1 : i));
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
      setFocusedIndex((i) => (i > 0 ? i - 1 : -1));
    } else if (e.key === 'Enter' && focusedIndex >= 0 && items[focusedIndex]) {
      e.preventDefault();
      const copyBtn = items[focusedIndex].querySelector('.common-qa-btn-copy');
      if (copyBtn) copyBtn.click();
    }
  };

  useEffect(() => {
    const el = resultListRef.current;
    if (!el) return;
    const item = el.querySelector(`[data-result-index="${focusedIndex}"]`);
    item?.scrollIntoView({ block: 'nearest', behavior: 'smooth' });
  }, [focusedIndex]);

  return (
    <div className="app-container common-qa-container">
      <Header backTo="/" backLabel="Back to Dashboard" />

      <main className="common-qa-main" onKeyDown={handleKeyDown}>
        <section className="common-qa-hero">
          <div className="common-qa-hero-inner">
            <span className="common-qa-hero-icon" aria-hidden>📚</span>
            <h1 className="common-qa-hero-title">Common Questions & Queries</h1>
            <p className="common-qa-hero-subtitle">
              Quickly find pre-built SumoLogic queries for common scenarios. No AI needed.
            </p>
          </div>
        </section>

        <section className="common-qa-search-section">
          <div className="common-qa-search-wrap">
            <input
              type="text"
              className="common-qa-search-input"
              placeholder="What are you looking for? (e.g., slow logins, API errors)"
              value={searchInput}
              onChange={(e) => setSearchInput(e.target.value)}
              aria-label="Search queries"
            />
            {searchInput && (
              <button
                type="button"
                className="common-qa-search-clear"
                onClick={() => setSearchInput('')}
                aria-label="Clear search"
              >
                ✕
              </button>
            )}
          </div>
          {hasSearch && (
            <p className="common-qa-results-count" role="status">
              {searching
                ? 'Searching…'
                : `Found ${searchResult.total} ${searchResult.total === 1 ? 'query' : 'queries'} matching '${debouncedSearch}'`}
            </p>
          )}
        </section>

        {hasSearch && (
          <section className="common-qa-results-section" ref={resultListRef}>
            {searching && displayQueries.length === 0 && (
              <p className="common-qa-loading">Searching…</p>
            )}
            {!searching && displayQueries.length === 0 && (
              <div className="common-qa-empty">
                <p>No results found. Try different keywords or browse categories below.</p>
                <button type="button" className="common-qa-btn-secondary" onClick={() => setSearchInput('')}>
                  Clear search
                </button>
              </div>
            )}
            {!searching && displayQueries.length > 0 && (
              <div className="common-qa-results-grid">
                {displayQueries.map((q, i) => (
                  <div key={q.id} data-result-index={i} className={focusedIndex === i ? 'common-qa-card-focused' : ''}>
                    <QueryCard
                      query={q}
                      searchTerm={debouncedSearch}
                      onCopy={() => showToast('Query copied!')}
                      onViewFull={setFullQueryModal}
                    />
                  </div>
                ))}
              </div>
            )}
          </section>
        )}

        {!hasSearch && (
          <>
            {popularQueries.length > 0 && (
              <section className="common-qa-popular">
                <h2 className="common-qa-section-title">⭐ Popular</h2>
                <div className="common-qa-results-grid common-qa-popular-grid">
                  {popularQueries.map((q) => (
                    <div key={q.id} className="common-qa-card common-qa-card-popular">
                      <span className="common-qa-badge common-qa-badge-popular">⭐ Popular</span>
                      {q.usageCount != null && (
                        <span className="common-qa-usage">Used {q.usageCount} times this month</span>
                      )}
                      <h3 className="common-qa-card-title">{q.name || 'Untitled'}</h3>
                      <pre className="common-qa-query-preview">
                        {(q.queryText || '').split('\n').slice(0, 2).join('\n')}
                        {(q.queryText || '').split('\n').length > 2 ? '\n...' : ''}
                      </pre>
                      <div className="common-qa-card-actions">
                        <button
                          type="button"
                          className="common-qa-btn-copy"
                          onClick={async () => {
                            try {
                              await navigator.clipboard.writeText(q.queryText || '');
                              showToast('Query copied!');
                              await incrementQueryUsage(q.id);
                            } catch (_) {}
                          }}
                        >
                          📋 Copy Query
                        </button>
                        <button type="button" className="common-qa-btn-view" onClick={() => setFullQueryModal(q)}>
                          View Full Query
                        </button>
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            )}

            <section className="common-qa-categories">
              <h2 className="common-qa-section-title">Browse by category</h2>
              <div className="common-qa-accordion">
                {categoryList.map((cat) => {
                  const count = byCategory[cat]?.length ?? 0;
                  const open = expandedCategories[cat] ?? false;
                  return (
                    <div key={cat} className="common-qa-accordion-item">
                      <button
                        type="button"
                        className="common-qa-accordion-trigger"
                        onClick={() => toggleCategory(cat)}
                        aria-expanded={open}
                      >
                        <span>{cat} ({count} queries)</span>
                        <span className="common-qa-accordion-chevron">{open ? '▼' : '▶'}</span>
                      </button>
                      <div className={`common-qa-accordion-panel ${open ? 'common-qa-accordion-panel-open' : ''}`}>
                        <div className="common-qa-results-grid">
                          {(byCategory[cat] || []).map((q) => (
                            <QueryCard
                              key={q.id}
                              query={q}
                              searchTerm=""
                              onCopy={() => showToast('Query copied!')}
                              onViewFull={setFullQueryModal}
                            />
                          ))}
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
              {CATEGORY_ORDER.every((c) => (byCategory[c]?.length ?? 0) === 0) && allQueries.length === 0 && (
                <p className="common-qa-empty-inline">No queries in the library yet. Contact your administrator to add queries.</p>
              )}
            </section>
          </>
        )}

        <section className="common-qa-quick-actions">
          <button type="button" className="common-qa-btn-request" onClick={() => setRequestModalOpen(true)}>
            Request New Query
          </button>
          <button type="button" className="common-qa-btn-all" onClick={() => setViewMode(viewMode === 'table' ? 'browse' : 'table')}>
            {viewMode === 'table' ? 'Show categories' : 'View All Queries'}
          </button>
        </section>

        {viewMode === 'table' && (
          <section className="common-qa-table-section">
            <h2 className="common-qa-section-title">All queries</h2>
            <div className="common-qa-table-wrap">
              <table className="admin-table common-qa-table">
                <thead>
                  <tr>
                    <th>Category</th>
                    <th>Key</th>
                    <th>Query (preview)</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {allQueries.map((q) => (
                    <tr key={q.id}>
                      <td><span className="common-qa-badge" data-category={q.category}>{q.category || '—'}</span></td>
                      <td>{q.name || '—'}</td>
                      <td><code className="admin-value-cell">{ (q.queryText || '').slice(0, 80) }…</code></td>
                      <td>
                        <button
                          type="button"
                          className="common-qa-btn-copy small"
                          onClick={async () => {
                            try {
                              await navigator.clipboard.writeText(q.queryText || '');
                              showToast('Query copied!');
                              await incrementQueryUsage(q.id);
                            } catch (_) {}
                          }}
                        >
                          Copy
                        </button>
                        <button type="button" className="common-qa-btn-view small" onClick={() => setFullQueryModal(q)}>
                          View
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        )}
      </main>

      {toast && (
        <div className="common-qa-toast" role="status">
          {toast}
        </div>
      )}

      {requestModalOpen && (
        <div className="modal-overlay" onClick={() => setRequestModalOpen(false)}>
          <div className="modal-content common-qa-request-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Request New Query</h2>
              <button type="button" className="close-button" onClick={() => setRequestModalOpen(false)} aria-label="Close">×</button>
            </div>
            <div className="modal-body">
              <p className="common-qa-request-hint">Describe the query you need. An admin will review and add it to the library.</p>
              <textarea
                className="common-qa-request-textarea"
                placeholder="e.g. Count 5xx errors by service in the last hour"
                rows={4}
                id="request-query-desc"
              />
            </div>
            <div className="modal-footer">
              <button type="button" className="button-secondary" onClick={() => setRequestModalOpen(false)}>Cancel</button>
              <button
                type="button"
                className="button-primary"
                onClick={() => {
                  const desc = document.getElementById('request-query-desc')?.value?.trim();
                  if (desc) showToast('Request submitted! Admins will review.');
                  setRequestModalOpen(false);
                }}
              >
                Submit request
              </button>
            </div>
          </div>
        </div>
      )}

      {fullQueryModal && (
        <div className="modal-overlay" onClick={() => setFullQueryModal(null)}>
          <div className="modal-content common-qa-full-query-modal" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>{fullQueryModal.name || 'Full query'}</h2>
              <button type="button" className="close-button" onClick={() => setFullQueryModal(null)} aria-label="Close">×</button>
            </div>
            <div className="modal-body">
              <pre className="common-qa-full-query-text">{fullQueryModal.queryText || ''}</pre>
              <button
                type="button"
                className="common-qa-btn-copy"
                onClick={async () => {
                  try {
                    await navigator.clipboard.writeText(fullQueryModal.queryText || '');
                    showToast('Query copied!');
                    await incrementQueryUsage(fullQueryModal.id);
                  } catch (_) {}
                }}
              >
                📋 Copy Query
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default CommonQAPage;
