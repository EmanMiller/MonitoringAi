import React, { useState, useEffect, useCallback } from 'react';
import { getLogMappings, createLogMapping, updateLogMapping, deleteLogMapping } from '../services/api';

const CATEGORIES = ['Environment', 'Intent'];

function AdminView() {
  const [items, setItems] = useState([]);
  const [loading, setLoading] = useState(true);
  const [toast, setToast] = useState(null);
  const [editingId, setEditingId] = useState(null);
  const [form, setForm] = useState({ category: CATEGORIES[0], key: '', value: '' });

  const load = useCallback(async () => {
    setLoading(true);
    try {
      const data = await getLogMappings();
      setItems(data);
    } catch (e) {
      setToast('Failed to load log mappings');
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
    setForm({ category: CATEGORIES[0], key: '', value: '' });
  };

  const handleEdit = (item) => {
    setEditingId(item.id);
    setForm({ category: item.category, key: item.key, value: item.value });
  };

  const handleSave = async () => {
    try {
      if (editingId) {
        await updateLogMapping(editingId, form);
        showToast('Updated');
      } else {
        await createLogMapping(form);
        showToast('Created');
      }
      setEditingId(null);
      setForm({ category: CATEGORIES[0], key: '', value: '' });
      load();
    } catch (e) {
      const msg = e.response?.data?.details || e.message || 'Failed';
      showToast(msg);
    }
  };

  const handleDelete = async (id) => {
    if (!confirm('Delete this mapping?')) return;
    try {
      await deleteLogMapping(id);
      showToast('Deleted');
      load();
    } catch (e) {
      showToast('Failed to delete');
    }
  };

  if (loading) return <div className="admin-loading">Loading...</div>;

  return (
    <div className="admin-view">
      {toast && <div className="admin-toast">{toast}</div>}
      <div className="admin-toolbar">
        <button type="button" className="button-primary" onClick={handleAdd}>
          Add Mapping
        </button>
      </div>
      <div className="admin-form-row">
        {(editingId || items.length === 0) && (
          <>
            <select
              value={form.category}
              onChange={(e) => setForm({ ...form, category: e.target.value })}
            >
              {CATEGORIES.map((c) => (
                <option key={c} value={c}>{c}</option>
              ))}
            </select>
            <input
              type="text"
              placeholder="Key (e.g. prod, qa)"
              value={form.key}
              onChange={(e) => setForm({ ...form, key: e.target.value })}
            />
            <input
              type="text"
              placeholder="Value (Sumo Logic fragment)"
              value={form.value}
              onChange={(e) => setForm({ ...form, value: e.target.value })}
            />
            <button type="button" className="button-primary" onClick={handleSave}>
              {editingId ? 'Update' : 'Create'}
            </button>
            {editingId && (
              <button type="button" className="button-secondary" onClick={handleAdd}>
                Cancel
              </button>
            )}
          </>
        )}
      </div>
      <table className="admin-table">
        <thead>
          <tr>
            <th>Category</th>
            <th>Key</th>
            <th>Value</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {items.map((item) => (
            <tr key={item.id}>
              <td>{item.category}</td>
              <td>{item.key}</td>
              <td>{item.value}</td>
              <td>
                <button type="button" onClick={() => handleEdit(item)}>Edit</button>
                <button type="button" onClick={() => handleDelete(item.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export default AdminView;
