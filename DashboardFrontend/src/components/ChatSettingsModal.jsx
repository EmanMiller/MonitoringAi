import React, { useState, useEffect } from 'react';

/**
 * Chat settings for backend-proxy mode. API key is never exposed to the frontend.
 */
const ChatSettingsModal = ({ isOpen, onClose, onConnectionChange, connectionStatus, onCheckStatus }) => {
  const [testing, setTesting] = useState(false);
  const [testError, setTestError] = useState('');
  const [testSuccess, setTestSuccess] = useState(false);

  useEffect(() => {
    if (isOpen) {
      setTestError('');
      setTestSuccess(false);
    }
  }, [isOpen]);

  const handleTestConnection = async () => {
    setTestError('');
    setTestSuccess(false);
    setTesting(true);
    try {
      const ok = onCheckStatus ? await onCheckStatus() : false;
      if (ok) {
        setTestSuccess(true);
        onConnectionChange?.(true);
      } else {
        setTestError('Chat is not configured or connection failed. Set Gemini:ApiKey on the server.');
        onConnectionChange?.(false);
      }
    } catch (err) {
      const msg = err.response?.data?.details || err.message || 'Connection failed.';
      setTestError(msg);
      onConnectionChange?.(false);
    } finally {
      setTesting(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content chat-settings-modal" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>Chat Settings</h2>
          <button type="button" className="close-button" onClick={onClose} aria-label="Close">
            ×
          </button>
        </div>
        <div className="modal-body">
          <p className="chat-settings-info">
            Chat is powered by the server. The Gemini API key is stored only on the backend and is never sent to the browser.
          </p>
          <p className="chat-settings-hint">
            To enable chat, an administrator must set <code>Gemini:ApiKey</code> in the server configuration (e.g. appsettings or environment).
          </p>
          <div className="chat-settings-actions">
            <button
              type="button"
              className="button-secondary"
              onClick={handleTestConnection}
              disabled={testing}
            >
              {testing ? 'Testing…' : 'Test Connection'}
            </button>
          </div>
          {testError && <p className="chat-settings-error">{testError}</p>}
          {testSuccess && <p className="chat-settings-success">Connection successful.</p>}
          <div className="chat-settings-status">
            <span className={`chat-status ${connectionStatus ? 'connected' : 'disconnected'}`}>
              <span className="chat-status-dot" />
              {connectionStatus ? 'Connected' : 'Not Connected'}
            </span>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ChatSettingsModal;
