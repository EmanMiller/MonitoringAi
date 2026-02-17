import React, { useState, useEffect, useCallback, useRef } from 'react';
import {
  getAllQueryLibrary,
  searchQueryLibrary,
  incrementQueryUsage,
  searchConfluence,
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
  const [activeCategory, setActiveCategory] = useState(null); // first category with queries
  const [toast, setToast] = useState(null);
  const [requestModalOpen, setRequestModalOpen] = useState(false);
  const [fullQueryModal, setFullQueryModal] = useState(null);
  const [focusedIndex, setFocusedIndex] = useState(-1);
  const resultListRef = useRef(null);

  // Confluence documentation search
  const [docSearchInput, setDocSearchInput] = useState('');
  const [docSearchDebounced, setDocSearchDebounced] = useState('');
  const [docResults, setDocResults] = useState([]);
  const [docSearching, setDocSearching] = useState(false);
  const [docShowMore, setDocShowMore] = useState(false);

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
    const t = setTimeout(() => setDocSearchDebounced(docSearchInput.trim()), 350);
    return () => clearTimeout(t);
  }, [docSearchInput]);

  useEffect(() => {
    if (!docSearchDebounced) {
      setDocResults([]);
      setDocSearching(false);
      return;
    }
    let cancelled = false;
    setDocSearching(true);
    searchConfluence(docSearchDebounced, 10)
      .then((res) => {
        if (cancelled) return;
        setDocResults(res?.results ?? []);
      })
      .catch(() => {
        if (!cancelled) setDocResults([]);
      })
      .finally(() => {
        if (!cancelled) setDocSearching(false);
      });
    return () => { cancelled = true; };
  }, [docSearchDebounced]);

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

  // Default activeCategory to first category with queries
  useEffect(() => {
    if (categoryList.length > 0 && activeCategory === null) {
      setActiveCategory(categoryList[0]);
    } else if (categoryList.length > 0 && !categoryList.includes(activeCategory)) {
      setActiveCategory(categoryList[0]);
    } else if (categoryList.length === 0) {
      setActiveCategory(null);
    }
  }, [categoryList, activeCategory]);

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
    <div className="common-qa-container">
      <main className="common-qa-main" onKeyDown={handleKeyDown}>
        <section className="common-qa-hero common-qa-hero-compact">
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
              <section className="common-qa-popular common-qa-popular-horizontal">
                <h2 className="common-qa-section-title">⭐ Popular</h2>
                <div className="common-qa-popular-row">
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

            <section className="common-qa-categories common-qa-categories-tabs">
              <h2 className="common-qa-section-title">Browse by category</h2>
              <div className="common-qa-tabs" role="tablist">
                {categoryList.map((cat) => {
                  const count = byCategory[cat]?.length ?? 0;
                  const isActive = activeCategory === cat;
                  return (
                    <button
                      key={cat}
                      type="button"
                      role="tab"
                      aria-selected={isActive}
                      className={`common-qa-tab ${isActive ? 'common-qa-tab-active' : ''}`}
                      onClick={() => setActiveCategory(cat)}
                    >
                      {cat} ({count})
                    </button>
                  );
                })}
              </div>
              {activeCategory && (
                <div className="common-qa-tab-panel" role="tabpanel">
                  <div className="common-qa-results-grid">
                    {(byCategory[activeCategory] || []).map((q) => (
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
              )}
              {CATEGORY_ORDER.every((c) => (byCategory[c]?.length ?? 0) === 0) && allQueries.length === 0 && (
                <p className="common-qa-empty-inline">No queries in the library yet. Contact your administrator to add queries.</p>
              )}
            </section>
          </>
        )}

        <section className="common-qa-documentation common-qa-doc-inline">
          <h2 className="common-qa-section-title">📄 Documentation (Confluence)</h2>
          <div className="common-qa-doc-search-wrap">
            <input
              type="text"
              className="common-qa-search-input common-qa-doc-input"
              placeholder="Search documentation (e.g., setup guide, API docs)"
              value={docSearchInput}
              onChange={(e) => setDocSearchInput(e.target.value)}
              aria-label="Search Confluence documentation"
            />
            {docSearching && <p className="common-qa-results-count">Searching…</p>}
            {!docSearching && docSearchDebounced && (
              <p className="common-qa-results-count" role="status">
                Found {docResults.length} {docResults.length === 1 ? 'page' : 'pages'}
              </p>
            )}
            {!docSearching && docResults.length > 0 && (
              <div className="common-qa-doc-results">
                {(docShowMore ? docResults : docResults.slice(0, 5)).map((r) => (
                  <a
                    key={r.id}
                    href={r.url}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="common-qa-doc-card"
                  >
                    <h4 className="common-qa-doc-card-title">{r.title || 'Untitled'}</h4>
                    {r.space && <span className="common-qa-badge">{r.space}</span>}
                    {r.excerpt && (
                      <p className="common-qa-doc-excerpt">{r.excerpt}</p>
                    )}
                  </a>
                ))}
                {docResults.length > 5 && !docShowMore && (
                  <button type="button" className="common-qa-show-more" onClick={() => setDocShowMore(true)}>
                    Show {docResults.length - 5} more
                  </button>
                )}
              </div>
            )}
            {!docSearching && docSearchDebounced && docResults.length === 0 && (
              <p className="common-qa-empty-inline">No documentation pages found. Try different keywords.</p>
            )}
          </div>
        </section>

        <section className="common-qa-quick-actions">
          <button type="button" className="common-qa-btn-request" onClick={() => setRequestModalOpen(true)}>
            Request New Query
          </button>
        </section>
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
