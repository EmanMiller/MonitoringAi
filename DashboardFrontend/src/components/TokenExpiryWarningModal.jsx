import React from 'react';
import { useAuth } from '../context/AuthContext';

export default function TokenExpiryWarningModal() {
  const { showExpiryWarning, setShowExpiryWarning, refreshToken, logout, logoutReason } = useAuth();

  if (!showExpiryWarning && !logoutReason) return null;

  if (logoutReason) {
    return (
      <div className="modal-overlay token-expiry-overlay" role="dialog" aria-modal="true" aria-labelledby="expiry-title">
        <div className="modal-content token-expiry-modal">
          <h2 id="expiry-title">Session expired</h2>
          <p>{logoutReason}</p>
          <button type="button" className="button-primary" onClick={() => window.location.assign('/login')}>
            Go to login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="modal-overlay token-expiry-overlay" role="dialog" aria-modal="true" aria-labelledby="expiry-warn-title">
      <div className="modal-content token-expiry-modal">
        <h2 id="expiry-warn-title">Session expiring soon</h2>
        <p>Your session will expire in about 5 minutes. Extend it to stay signed in.</p>
        <div className="token-expiry-actions">
          <button type="button" className="button-secondary" onClick={() => setShowExpiryWarning(false)}>
            Dismiss
          </button>
          <button
            type="button"
            className="button-primary"
            onClick={async () => {
              const ok = await refreshToken();
              if (ok) setShowExpiryWarning(false);
            }}
          >
            Extend session
          </button>
        </div>
      </div>
    </div>
  );
}
