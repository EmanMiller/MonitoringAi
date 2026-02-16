import React, { useState, useEffect, useCallback } from 'react';
import {
  getQueryLibrary,
  createQueryLibraryItem,
  updateQueryLibraryItem,
  deleteQueryLibraryItem,
  exportQueryLibrary,
  importQueryLibrary,
} from '../services/api';
import { useAuth } from '../context/AuthContext';
import { canCreate, canUpdate, canDelete, canAddCustomCategory } from '../constants/roles';
import {
  PREDEFINED_CATEGORIES,
  CATEGORY_NAMES,
  getCategoryColor,
  KEY_MAX_LENGTH,
  SUGGESTED_TAGS,
} from '../constants/queryLibraryCategories';

function validateKey(key) {
  const t = key.trim();
  if (!t) return { valid: false, message: 'Key is required.' };
  if (t.length > KEY_MAX_LENGTH) return { valid: false, message: `Key must be at most ${KEY_MAX_LENGTH} characters.` };
  const looksLikeQuestion = /\?$/.test(t) || /^(how|what|why|when|where|which|who|i need|show me|get me|find)/i.test(t) || t.length > 10;
  if (!looksLikeQuestion) return { valid: false, message: 'Key should be a question or statement (e.g. "I need a query for slow logins").' };
  return { valid: true };
}

function QueryLibraryView() {
  const { user } = useAuth();
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [toast, setToast] = useState(null);
  const [categoryFilter, setCategoryFilter] = useState('');
  const [editingId, setEditingId] = useState(null);
  const [isAdding, setIsAdding] = useState(false);
  const [customCategories, setCustomCategories] = useState([]);
  const [form, setForm] = useState({
    category: CATEGORY_NAMES[0] || '',
    key: '',
    value: '',
    tags: [],
  });
  const [tagInput, setTagInput] = useState('');
  const [keyError, setKeyError] = useState(null);
  const [customCategoryName, setCustomCategoryName] = useState('');

  const allCategories = [...CATEGORY_NAMES, ...customCategories];
  const canCreateItem = canCreate(user?.role);
  const canUpdateItem = canUpdate(user?.role);
  const canDeleteItem = canDelete(user?.role);
  const showAddCustomCategory = canAddCustomCategory(user?.role);

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getQueryLibrary();
      setItems(data);
      const custom = [...new Set(data.map((x) => x.category).filter((c) => !CATEGORY_NAMES.includes(c)))];
      setCustomCategories(custom);
    } catch (e) {
      setToast('Failed to load query library');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const showToast = (msg) => {
    setToast(msg);
    setTimeout(() => setToast(null), 3000);
  };

  const handleAdd = () => {
    setEditingId(null);
    setForm({ category: allCategories[0] || '', key: '', value: '', tags: [] });
    setCustomCategoryName('');
    setKeyError(null);
    setIsAdding(true);
  };

  const handleEdit = (row) => {
    if (!canUpdateItem) return;
    setIsAdding(false);
    setEditingId(row.id);
    setForm({
      category: row.category,
      key: row.key,
      value: row.value,
      tags: Array.isArray(row.tags) ? row.tags : (row.tagsJson ? JSON.parse(row.tagsJson || '[]') : []),
    });
    setKeyError(null);
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setIsAdding(false);
    setKeyError(null);
  };

  const handleKeyBlur = () => {
    if (!form.key.trim()) setKeyError(null);
    else setKeyError(validateKey(form.key).message);
  };

  const resolveCategory = () => (form.category === '__custom__' ? customCategoryName.trim() || form.category : form.category);

  const handleSaveNew = async () => {
    const v = validateKey(form.key);
    if (!v.valid) {
      setKeyError(v.message);
      return;
    }
    if (form.category === '__custom__' && !customCategoryName.trim()) {
      setKeyError('Enter a custom category name.');
      return;
    }
    if (!canCreateItem) return;
    try {
      await createQueryLibraryItem({
        category: resolveCategory(),
        key: form.key.trim(),
        value: form.value,
        tags: form.tags,
        createdBy: user?.id || 'developer',
        roleRequired: 'developer',
      });
      showToast('Saved successfully');
      handleCancelEdit();
      load();
    } catch {
      showToast('Failed to save');
    }
  };

  const handleSaveEdit = async () => {
    const v = validateKey(form.key);
    if (!v.valid) {
      setKeyError(v.message);
      return;
    }
    if (form.category === '__custom__' && !customCategoryName.trim()) {
      setKeyError('Enter a custom category name.');
      return;
    }
    if (!editingId || !canUpdateItem) return;
    try {
      await updateQueryLibraryItem(editingId, {
        category: resolveCategory(),
        key: form.key.trim(),
        value: form.value,
        tags: form.tags,
        roleRequired: 'developer',
      });
      showToast('Updated successfully');
      handleCancelEdit();
      load();
    } catch {
      showToast('Failed to update');
    }
  };

  const handleDelete = async (id) => {
    if (!canDeleteItem) return;
    if (!window.confirm('Delete this query?')) return;
    try {
      await deleteQueryLibraryItem(id);
      showToast('Deleted');
      if (editingId === id) handleCancelEdit();
      load();
    } catch {
      showToast('Failed to delete');
    }
  };

  const addTag = (tag) => {
    const t = (tag || tagInput).trim().toLowerCase();
    if (!t || form.tags.includes(t)) return;
    setForm((f) => ({ ...f, tags: [...f.tags, t] }));
    setTagInput('');
  };

  const removeTag = (tag) => {
    setForm((f) => ({ ...f, tags: f.tags.filter((x) => x !== tag) }));
  };

  const handleExport = async () => {
    try {
      const data = await exportQueryLibrary();
      const blob = new Blob([JSON.stringify(data, null, 2)], { type: 'application/json' });
      const a = document.createElement('a');
      a.href = URL.createObjectURL(blob);
      a.download = `query-library-export-${new Date().toISOString().slice(0, 10)}.json`;
      a.click();
      URL.revokeObjectURL(a.href);
      showToast('Exported');
    } catch {
      showToast('Export failed');
    }
  };

  const handleImport = () => {
    const input = document.createElement('input');
    input.type = 'file';
    input.accept = '.json';
    input.onchange = async (e) => {
      const file = e.target?.files?.[0];
      if (!file) return;
      try {
        const text = await file.text();
        const payload = JSON.parse(text);
        const res = await importQueryLibrary(payload);
        showToast(`Imported ${res.importedCount} items`);
        load();
      } catch {
        showToast('Import failed');
      }
    };
    input.click();
  };

  const filteredItems = categoryFilter
    ? items.filter((i) => i.category === categoryFilter)
    : items;

  const parseTags = (item) => {
    if (Array.isArray(item.tags)) return item.tags;
    try {
      return item.tagsJson ? JSON.parse(item.tagsJson) : [];
    } catch {
      return [];
    }
  };

  return (
    <div className="query-library-view">
      <div className="ql-header">
        <h1>Query Library</h1>
        <p className="ql-subtitle">
          Manage saved Sumo Logic queries by category. Key = search intent; Value = query text.
        </p>
        <div className="ql-actions">
          {canCreateItem && !isAdding && !editingId && (
            <button type="button" className="btn-primary" onClick={handleAdd}>
              Add query
            </button>
          )}
          <button type="button" className="btn-secondary" onClick={handleExport}>
            Export
          </button>
          <button type="button" className="btn-secondary" onClick={handleImport}>
            Import
          </button>
        </div>
      </div>

      {toast && <div className="ql-toast" role="status">{toast}</div>}

      {/* Category management */}
      <section className="ql-section">
        <h2>Category</h2>
        <div className="ql-categories">
          {PREDEFINED_CATEGORIES.map((c) => (
            <span
              key={c.name}
              className="ql-category-badge"
              style={{ backgroundColor: c.color + '33', color: c.color, borderColor: c.color }}
            >
              {c.name}
            </span>
          ))}
          {customCategories.map((c) => (
            <span
              key={c}
              className="ql-category-badge ql-category-custom"
              style={{ backgroundColor: '#64748b33', color: '#94a3b8', borderColor: '#64748b' }}
            >
              {c}
            </span>
          ))}
          {showAddCustomCategory && (
            <span className="ql-category-badge ql-add-custom" title="Add custom category when creating/editing a query">
              + Custom (use in form)
            </span>
          )}
        </div>
      </section>

      {/* Add/Edit form */}
      {(isAdding || editingId) && (
        <div className="ql-form-card">
          <h3>{editingId ? 'Edit query' : 'New query'}</h3>
          <div className="ql-form-grid">
            <div className="ql-field">
              <label>Category (required)</label>
              <select
                value={form.category}
                onChange={(e) => setForm((f) => ({ ...f, category: e.target.value }))}
              >
                {allCategories.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
                {showAddCustomCategory && (
                  <option value="__custom__">-- Add custom category --</option>
                )}
              </select>
              {form.category === '__custom__' && (
                <input
                  type="text"
                  placeholder="Custom category name"
                  className="ql-input"
                  value={customCategoryName}
                  onChange={(e) => setCustomCategoryName(e.target.value)}
                />
              )}
            </div>
            <div className="ql-field ql-field-full">
              <label>Key (search intent)</label>
              <input
                type="text"
                className="ql-input"
                value={form.key}
                onChange={(e) => setForm((f) => ({ ...f, key: e.target.value.slice(0, KEY_MAX_LENGTH) }))}
                onBlur={handleKeyBlur}
                placeholder="e.g., I need a query for slow logins"
                maxLength={KEY_MAX_LENGTH}
              />
              <span className="ql-char-count">{form.key.length}/{KEY_MAX_LENGTH}</span>
              {keyError && <span className="ql-error">{keyError}</span>}
            </div>
            <div className="ql-field ql-field-full">
              <label>Value (Sumo Logic query)</label>
              <textarea
                className="ql-code-editor"
                value={form.value}
                onChange={(e) => setForm((f) => ({ ...f, value: e.target.value }))}
                placeholder="Enter SumoLogic query here"
                spellCheck={false}
              />
            </div>
            <div className="ql-field ql-field-full">
              <label>Tags (optional)</label>
              <div className="ql-tags-row">
                {SUGGESTED_TAGS.map((t) => (
                  <button
                    key={t}
                    type="button"
                    className={`ql-tag-chip ${form.tags.includes(t) ? 'active' : ''}`}
                    onClick={() => addTag(t)}
                  >
                    {t}
                  </button>
                ))}
                <input
                  type="text"
                  className="ql-tag-input"
                  value={tagInput}
                  onChange={(e) => setTagInput(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && (e.preventDefault(), addTag())}
                  placeholder="Add tag..."
                />
              </div>
              {form.tags.length > 0 && (
                <div className="ql-tags-selected">
                  {form.tags.map((t) => (
                    <span key={t} className="ql-tag-selected">
                      {t}
                      <button type="button" aria-label={`Remove ${t}`} onClick={() => removeTag(t)}>×</button>
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
          <div className="ql-form-actions">
            <button type="button" className="btn-secondary" onClick={handleCancelEdit}>
              Cancel
            </button>
            {editingId ? (
              <button type="button" className="btn-primary" onClick={handleSaveEdit} disabled={!!keyError}>
                Save changes
              </button>
            ) : (
              <button type="button" className="btn-primary" onClick={handleSaveNew} disabled={!!keyError}>
                Add
              </button>
            )}
          </div>
        </div>
      )}

      {/* Table */}
      <section className="ql-section">
        <div className="ql-table-header">
          <h2>Queries</h2>
          <select
            className="ql-filter-select"
            value={categoryFilter}
            onChange={(e) => setCategoryFilter(e.target.value)}
          >
            <option value="">All categories</option>
            {allCategories.map((c) => (
              <option key={c} value={c}>{c}</option>
            ))}
          </select>
        </div>
        <div className="ql-table-wrap">
          {loading ? (
            <p className="ql-loading">Loading…</p>
          ) : (
            <table className="ql-table">
              <thead>
                <tr>
                  <th>Category</th>
                  <th>Key</th>
                  <th>Value</th>
                  <th>Created By | Created Date</th>
                  <th className="ql-th-actions">Actions</th>
                </tr>
              </thead>
              <tbody>
                {filteredItems.length === 0 ? (
                  <tr>
                    <td colSpan={5} className="ql-empty">No queries yet. Add one above.</td>
                  </tr>
                ) : (
                  filteredItems.map((row) => (
                    <tr key={row.id}>
                      <td>
                        <span
                          className="ql-category-pill"
                          style={{
                            backgroundColor: getCategoryColor(row.category) + '33',
                            color: getCategoryColor(row.category),
                            borderColor: getCategoryColor(row.category),
                          }}
                        >
                          {row.category}
                        </span>
                      </td>
                      <td className="ql-cell-key">{row.key}</td>
                      <td><code className="ql-value-cell">{row.value}</code></td>
                      <td className="ql-cell-meta">
                        {row.createdBy} | {row.createdAt ? new Date(row.createdAt).toLocaleString() : '—'}
                      </td>
                      <td className="ql-actions-cell">
                        {canUpdateItem && (
                          <button type="button" className="ql-btn ql-btn-edit" onClick={() => handleEdit(row)}>
                            Edit
                          </button>
                        )}
                        {canDeleteItem && (
                          <button type="button" className="ql-btn ql-btn-delete" onClick={() => handleDelete(row.id)}>
                            Delete
                          </button>
                        )}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </div>
  );
}

export default QueryLibraryView;
