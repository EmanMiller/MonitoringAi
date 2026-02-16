import React, { useState, useEffect } from 'react';
import {
  getLogMappings,
  createLogMapping,
  updateLogMapping,
  deleteLogMapping,
} from '../services/api';

const CATEGORIES = ['Environment', 'Intent'];

function AdminView() {
  const [mappings, setMappings] = useState([]);
  const [loading, setLoading] = useState(true);
  const [saveNotification, setSaveNotification] = useState(null);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState({ category: 'Environment', key: '', value: '' });
  const [isAdding, setIsAdding] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const data = await getLogMappings();
      setMappings(data);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const showSaved = () => {
    setSaveNotification('Saved successfully');
    setTimeout(() => setSaveNotification(null), 3000);
  };

  const handleAdd = () => {
    setEditingId(null);
    setForm({ category: 'Environment', key: '', value: '' });
    setIsAdding(true);
  };

  const handleEdit = (row) => {
    setIsAdding(false);
    setEditingId(row.id);
    setForm({
      category: row.category,
      key: row.key,
      value: row.value,
    });
  };

  const handleCancelEdit = () => {
    setEditingId(null);
    setIsAdding(false);
    setForm({ category: 'Environment', key: '', value: '' });
  };

  const handleSaveNew = async () => {
    if (!form.key.trim()) return;
    await createLogMapping({
      category: form.category,
      key: form.key.trim(),
      value: form.value.trim(),
    });
    showSaved();
    handleCancelEdit();
    load();
  };

  const handleSaveEdit = async () => {
    if (!form.key.trim() || editingId == null) return;
    await updateLogMapping(editingId, {
      category: form.category,
      key: form.key.trim(),
      value: form.value.trim(),
    });
    showSaved();
    handleCancelEdit();
    load();
  };

  const handleDelete = async (id) => {
    if (!window.confirm('Delete this mapping?')) return;
    await deleteLogMapping(id);
    showSaved();
    if (editingId === id) handleCancelEdit();
    load();
  };

  return (
    <div className="admin-view">
      <div className="admin-header">
        <h1>Log Mappings (Admin)</h1>
        <p className="admin-subtitle">Manage how the AI interprets company log terms. Category: Environment or Intent. Key = user word, Value = Sumo Logic string.</p>
        <div className="admin-actions">
          {!isAdding && !editingId && (
            <button type="button" className="button-primary" onClick={handleAdd}>
              Add mapping
            </button>
          )}
        </div>
      </div>

      {saveNotification && (
        <div className="admin-toast" role="status">
          {saveNotification}
        </div>
      )}

      {(isAdding || editingId !== null) && (
        <div className="admin-form-card">
          <h3>{editingId ? 'Edit mapping' : 'New mapping'}</h3>
          <div className="admin-form-grid">
            <div className="admin-field">
              <label>Category</label>
              <select
                value={form.category}
                onChange={(e) => setForm({ ...form, category: e.target.value })}
              >
                {CATEGORIES.map((c) => (
                  <option key={c} value={c}>{c}</option>
                ))}
              </select>
            </div>
            <div className="admin-field">
              <label>Key</label>
              <input
                type="text"
                value={form.key}
                onChange={(e) => setForm({ ...form, key: e.target.value })}
                placeholder="e.g. prod, qa"
              />
            </div>
            <div className="admin-field admin-field-full">
              <label>Value</label>
              <input
                type="text"
                value={form.value}
                onChange={(e) => setForm({ ...form, value: e.target.value })}
                placeholder="Sumo Logic string / query fragment"
              />
            </div>
          </div>
          <div className="admin-form-actions">
            <button type="button" className="button-secondary" onClick={handleCancelEdit}>
              Cancel
            </button>
            {editingId ? (
              <button type="button" className="button-primary" onClick={handleSaveEdit} disabled={!form.key.trim()}>
                Save changes
              </button>
            ) : (
              <button type="button" className="button-primary" onClick={handleSaveNew} disabled={!form.key.trim()}>
                Add
              </button>
            )}
          </div>
        </div>
      )}

      <div className="admin-table-wrap">
        {loading ? (
          <p className="admin-loading">Loading…</p>
        ) : (
          <table className="admin-table">
            <thead>
              <tr>
                <th>Category</th>
                <th>Key</th>
                <th>Value</th>
                <th className="admin-th-actions">Actions</th>
              </tr>
            </thead>
            <tbody>
              {mappings.length === 0 ? (
                <tr>
                  <td colSpan={4} className="admin-empty">No mappings yet. Add one above.</td>
                </tr>
              ) : (
                mappings.map((row) => (
                  <tr key={row.id}>
                    <td>{row.category}</td>
                    <td>{row.key}</td>
                    <td><code className="admin-value-cell">{row.value}</code></td>
                    <td className="admin-actions-cell">
                      <button type="button" className="admin-btn admin-btn-edit" onClick={() => handleEdit(row)}>Edit</button>
                      <button type="button" className="admin-btn admin-btn-delete" onClick={() => handleDelete(row.id)}>Delete</button>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}

export default AdminView;
