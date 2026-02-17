import React, { useState } from 'react';
import { simulateAddToConfluence } from '../services/api';

const SimulateConfluenceModal = ({ isOpen, onClose }) => {
  const [dashboardName, setDashboardName] = useState('');
  const [projectName, setProjectName] = useState('');
  const [pageId, setPageId] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null); // { success, pageUrl, error }

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!dashboardName.trim() || !projectName.trim()) return;
    setSubmitting(true);
    setResult(null);
    try {
      const res = await simulateAddToConfluence({
        dashboardName: dashboardName.trim(),
        projectName: projectName.trim(),
        ...(pageId.trim() && { confluencePageId: pageId.trim() }),
      });
      setResult({ success: true, pageUrl: res.pageUrl, message: res.message });
      setDashboardName('');
      setProjectName('');
      setPageId('');
    } catch (err) {
      const details = err?.response?.data;
      const errorMsg = details?.error || details?.details || err.message || 'Failed to add to Confluence.';
      setResult({ success: false, error: errorMsg });
    } finally {
      setSubmitting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Simulate Add to Confluence</h2>
          <button type="button" className="close-button" onClick={onClose} aria-label="Close">×</button>
        </div>
        <div className="modal-body">
          <p className="modal-hint">
            Add a dashboard row to your Confluence tracking page without creating a real Sumo Logic dashboard. Useful for testing.
          </p>
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="sim-dashboard">Dashboard name *</label>
              <input
                id="sim-dashboard"
                type="text"
                value={dashboardName}
                onChange={(e) => setDashboardName(e.target.value)}
                placeholder="e.g. Q4 Revenue Dashboard"
                required
                disabled={submitting}
              />
            </div>
            <div className="form-group">
              <label htmlFor="sim-project">Project name *</label>
              <input
                id="sim-project"
                type="text"
                value={projectName}
                onChange={(e) => setProjectName(e.target.value)}
                placeholder="e.g. Revenue Team"
                required
                disabled={submitting}
              />
            </div>
            <div className="form-group">
              <label htmlFor="sim-page">Confluence page ID (optional)</label>
              <input
                id="sim-page"
                type="text"
                value={pageId}
                onChange={(e) => setPageId(e.target.value)}
                placeholder="Uses CONFLUENCE_PAGE_ID from .env if blank"
                disabled={submitting}
              />
            </div>
            {result && (
              <div className={`simulate-result ${result.success ? 'success' : 'error'}`} role="status">
                {result.success ? (
                  <>
                    <p>{result.message}</p>
                    {result.pageUrl && (
                      <a href={result.pageUrl} target="_blank" rel="noopener noreferrer" className="simulate-link">
                        View in Confluence →
                      </a>
                    )}
                  </>
                ) : (
                  <p>{result.error}</p>
                )}
              </div>
            )}
            <div className="modal-footer">
              <button type="button" className="button-secondary" onClick={onClose}>Cancel</button>
              <button type="submit" className="button-primary" disabled={submitting}>
                {submitting ? 'Adding…' : 'Add to Confluence'}
              </button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default SimulateConfluenceModal;
